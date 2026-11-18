// Imports React primitives for form events, effects, memoized totals, and local UI state.
import { FormEvent, useEffect, useMemo, useState } from 'react'
// Imports TanStack Query hooks to separate server state from UI state.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
// Imports structured API errors and typed product models from the centralized client.
import { EcommerceApiError, Product, ecommerceApi } from './api'
import { useAuth } from './lib/AuthContext'
import CustomerMenu from '../components/store/CustomerMenu'
import NotificationBell from '../components/store/NotificationBell'
// Imports Zod so the checkout payload is validated before transmission.
import { z } from 'zod'

// Represents one browser cart row and its associated API product contract.
interface CartItem {
  // Keeps the product response needed for display and identity.
  product: Product
  // Keeps the quantity that will be submitted with the order.
  quantity: number
}

// Validates the complete order payload before it crosses the network boundary.
const orderSchema = z.object({
  // Requires the demo customer identifier to be a UUID.
  customerId: z.string().uuid(),
  shippingProvinceId: z.string().uuid().optional(),
  // Requires freight to be present and nonnegative.
  orderFreight: z.number().nonnegative(),
  // Requires the order discount to be present and nonnegative.
  discountAmount: z.number().nonnegative(),
  // Requires a string comment field.
  comment: z.string(),
  // Requires the creator audit field to be a string.
  createdBy: z.string(),
  // Requires the modifier audit field to be a string.
  modifiedBy: z.string(),
  // Requires at least one detailed order line.
  orderDetail: z.array(z.object({
    // Requires each product identifier to be a UUID.
    productId: z.string().uuid(),
    // Requires each unit-of-measure identifier to be a UUID.
    uomId: z.string().uuid(),
    // Requires positive quantity because an order line cannot be empty or negative.
    quantity: z.number().positive(),
    // Requires a nonnegative unit price.
    unitPrice: z.number().nonnegative(),
    // Requires nonnegative line freight.
    itemFreight: z.number().nonnegative(),
    // Requires nonnegative line discount.
    discountAmount: z.number().nonnegative(),
    // Requires a string comment for the order detail.
    comment: z.string(),
  })).min(1),
})

// Defines the API-backed storefront component and its feature interactions.
export default function App() {
  const { user, logout } = useAuth()
  // Tracks the currently selected page tab.
  const [tab, setTab] = useState<'shop' | 'cart' | 'locations'>('shop')
  // Tracks the product search field value.
  const [search, setSearch] = useState('')
  // Tracks user-visible API or validation errors.
  const [error, setError] = useState('')
  // Loads the cart from browser storage and tracks subsequent cart changes.
  const [cart, setCart] = useState<CartItem[]>([])
  const [cartUserId, setCartUserId] = useState<string | null>(null)
  // Tracks the selected province identifier.
  const [provinceId, setProvinceId] = useState('')
  // Tracks the selected city identifier.
  const [cityId, setCityId] = useState('')
  // Tracks a successful checkout notification.
  const [orderMessage, setOrderMessage] = useState('')

  const profileQuery = useQuery({
    queryKey: ['customer-profile', user?.customerId],
    queryFn: ecommerceApi.getCustomerProfile,
    enabled: Boolean(user?.customerId),
  })
  // Caches product search results and exposes loading and error states.
  const productsQuery = useQuery({
    // Separates cache entries for search and selected delivery province.
    queryKey: ['products', search, provinceId],
    // Requests products while forwarding React Query cancellation.
    queryFn: ({ signal }) => ecommerceApi.getProducts(search, undefined, 1, signal, provinceId || undefined),
  })
  const ordersQuery = useQuery({
    queryKey: ['orders', user?.customerId],
    queryFn: () => ecommerceApi.getOrders(),
    enabled: Boolean(user?.customerId),
  })
  // Caches the province list shared by the location selector.
  const provincesQuery = useQuery({
    // Uses one stable province cache entry.
    queryKey: ['provinces'],
    // Fetches provinces through the centralized API client.
    queryFn: ({ signal }) => ecommerceApi.getProvinces(signal),
  })
  // Caches cities only after a province is selected.
  const citiesQuery = useQuery({
    // Separates city results for every province.
    queryKey: ['cities', provinceId],
    // Fetches cities for the selected province.
    queryFn: ({ signal }) => ecommerceApi.getCities(provinceId, signal),
    // Prevents an empty province identifier from producing a request.
    enabled: Boolean(provinceId),
  })
  // Caches barangays only after both parent identifiers are selected.
  const barangaysQuery = useQuery({
    // Separates barangay results for each province-city pair.
    queryKey: ['barangays', provinceId, cityId],
    // Fetches barangays for the selected province and city.
    queryFn: ({ signal }) => ecommerceApi.getBarangays(provinceId, cityId, signal),
    // Prevents requests until the required identifiers are selected.
    enabled: Boolean(provinceId && cityId),
  })
  // Gives the order mutation access to the application cache.
  const queryClient = useQueryClient()
  // Defines the order submission lifecycle and cache invalidation behavior.
  const orderMutation = useMutation({
    // Sends the validated order through the API client.
    mutationFn: (order: unknown) => ecommerceApi.createOrder(order),
    // Refreshes product server state after a successful order.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })

  // Persists browser cart changes after every quantity or item update.
  useEffect(() => {
    if (!user?.id) return
    try {
      const saved = localStorage.getItem(`ecommerce-cart:${user.id}`)
      setCart(saved ? JSON.parse(saved) : [])
    } catch {
      setCart([])
    }
    setCartUserId(user.id)
  }, [user?.id])

  useEffect(() => {
    if (user?.id && cartUserId === user.id) {
      localStorage.setItem(`ecommerce-cart:${user.id}`, JSON.stringify(cart))
    }
  }, [cart, cartUserId, user?.id])

  // Calculates the cart total whenever cart contents change.
  const cartTotal = useMemo(
    // Sums each line by its API-configured price.
    () => cart.reduce((sum, item) => sum + (item.product.defaultPrice?.price ?? 0) * item.quantity, 0),
    // Recalculates only when the cart changes.
    [cart],
  )

  // Adds a product to the cart or increments an existing cart line.
  function addToCart(product: Product) {
    // Updates cart state immutably for React rendering.
    setCart(current => {
      // Finds an existing row for this product.
      const existing = current.find(item => item.product.productId === product.productId)
      // Increments that row instead of duplicating it.
      if (existing) {
        // Maps the matching row to a quantity increment.
        return current.map(item => item.product.productId === product.productId
          // Preserves the row and adds one unit.
          ? { ...item, quantity: item.quantity + 1 }
          // Leaves unrelated rows unchanged.
          : item)
      }
      // Appends a new quantity-one row for an unseen product.
      return [...current, { product, quantity: 1 }]
    })
  }

  // Changes a cart quantity or removes zero and negative quantities.
  function updateQuantity(productId: string, quantity: number) {
    // Removes the line when its quantity is no longer positive.
    if (quantity <= 0) {
      // Filters the removed product out of the cart.
      setCart(current => current.filter(item => item.product.productId !== productId))
      // Stops quantity updates after removal.
      return
    }
    // Updates only the matching product line.
    setCart(current => current.map(item =>
      item.product.productId === productId ? { ...item, quantity } : item))
  }

  // Validates and submits the browser cart as an API order.
  async function checkout(event: FormEvent) {
    // Prevents the form from performing a full-page reload.
    event.preventDefault()
    // Prevents an empty cart from creating an invalid order.
    if (!cart.length) return
    // Clears stale error text before submission.
    setError('')
    // Clears stale success text before submission.
    setOrderMessage('')
    if (!user?.customerId) {
      setError('This account is not linked to a customer profile and cannot place orders.')
      return
    }
    if (cart.some(item => !item.product.defaultPrice)) {
      setError('A product in your cart does not have a configured price.')
      return
    }
    try {
      // Converts demo cart state into the Zod-validated API payload.
      const order = orderSchema.parse({
        // Supplies the seeded demo customer.
        customerId: user.customerId,
        shippingProvinceId: provinceId || profileQuery.data?.provinceId || undefined,
        // Starts freight at zero because no backend shipping calculation is integrated.
        orderFreight: 0,
        // Starts the order discount at zero.
        discountAmount: 0,
        // Records the checkout source in the order comment.
        comment: 'Created from Ecommerce.Main',
        // Supplies the demo creator audit value.
        createdBy: user.username,
        // Supplies the demo modifier audit value.
        modifiedBy: user.username,
        // Maps cart rows into backend order-detail fields.
        orderDetail: cart.map(item => ({
          // Sends the selected product identifier.
          productId: item.product.productId,
          // Sends the unit selected by the API's default configured price.
          uomId: item.product.defaultPrice!.uomId,
          // Sends the cart quantity.
          quantity: item.quantity,
          // Sends the displayed value; the API recalculates from its price table.
          unitPrice: item.product.defaultPrice!.price,
          // Starts line freight at zero.
          itemFreight: 0,
          // Starts line discount at zero.
          discountAmount: 0,
          // Sends an empty optional line comment.
          comment: '',
        })),
      })
      // Executes the mutation and waits for the API response.
      await orderMutation.mutateAsync(order)
      // Clears the browser cart after successful persistence.
      setCart([])
      // Announces checkout success to the user.
      setOrderMessage('Order created successfully.')
    } catch (cause) {
      // Shows a backend detail or a safe general checkout failure.
      setError(cause instanceof EcommerceApiError ? cause.apiError.detail : 'Unable to create order.')
    }
  }

  // Returns the React storefront structure.
  return (
    // Provides the top-level document container.
    <main>
      {/* Displays the product name and navigation tabs. */}
      <header className="topbar">
        {/* Names the storefront. */}
        <h1>Ecommerce Main</h1>
        {/* Allows switching among shop, cart, and location views. */}
        <nav>
          <NotificationBell />
          <CustomerMenu
            user={user}
            customer={profileQuery.data}
            orders={ordersQuery.data?.items ?? []}
            onCustomerUpdate={updated => queryClient.setQueryData(['customer-profile', user?.customerId], updated)}
            onLogout={logout}
          />
          {/* Switches to the shop view and marks it active. */}
          <button className={tab === 'shop' ? 'active' : ''} onClick={() => setTab('shop')}>Shop</button>
          {/* Switches to the cart view and shows its item count. */}
          <button className={tab === 'cart' ? 'active' : ''} onClick={() => setTab('cart')}>Cart ({cart.length})</button>
          {/* Switches to the location selector view. */}
          <button className={tab === 'locations' ? 'active' : ''} onClick={() => setTab('locations')}>Locations</button>
        </nav>
      </header>

      {/* Displays API and validation failures as an alert. */}
      {error && <p className="alert" role="alert">{error}</p>}
      {/* Displays successful checkout feedback as status text. */}
      {orderMessage && <p className="success" role="status">{orderMessage}</p>}

      {/* Renders the shop view when shop is selected. */}
      {tab === 'shop' && (
        // Groups catalog controls and product cards.
        <section>
          {/* Provides the search control. */}
          <div className="toolbar">
            {/* Associates the search label with the input. */}
            <label>
              {/* Names the search field. */}
              Search products
              {/* Updates the query cache key as the search term changes. */}
              <input value={search} onChange={event => setSearch(event.target.value)} placeholder="Search..." />
            </label>
          </div>
          {/* Shows loading, error, or the returned product grid. */}
          {productsQuery.isPending ? <p>Loading products...</p> : productsQuery.isError ? (
            // Exposes a backend or generic product-loading error.
            <p role="alert">{productsQuery.error instanceof EcommerceApiError ? productsQuery.error.apiError.detail : 'Unable to load products.'}</p>
          ) : (
            // Displays returned products in a responsive card grid.
            <div className="grid">
              {/* Maps each API product into a reusable card. */}
              {productsQuery.data?.items.map(product => (
                // Identifies the card by the stable product GUID.
                <article className="card" key={product.productId}>
                  {/* Adds a text-based visual marker for the product. */}
                  <div className="product-mark" aria-hidden="true">{product.productName[0]}</div>
                  {/* Shows the product name. */}
                  <h2>{product.productName}</h2>
                  {/* Shows the product description. */}
                  <p>{product.productDescription}</p>
                  {/* Shows the configured price and unit or explains missing configuration. */}
                  <strong>{product.defaultPrice ? `$${product.defaultPrice.price.toFixed(2)} / ${product.defaultPrice.unit}` : 'Price not set'}</strong>
                  {/* Adds or disables cart action based on inventory state. */}
                  <button disabled={!product.inStock || !product.defaultPrice} onClick={() => addToCart(product)}>
                    {!product.inStock ? 'Out of stock' : product.defaultPrice ? 'Add to cart' : 'Unavailable'}
                  </button>
                </article>
              ))}
            </div>
          )}
        </section>
      )}

      {/* Renders the cart and checkout view when cart is selected. */}
      {tab === 'cart' && (
        // Groups cart state and order summary.
        <section>
          {/* Names the cart page. */}
          <h2>Cart</h2>
          {/* Shows an empty state or a submission form. */}
          {!cart.length ? <p>Your cart is empty.</p> : (
            // Submits the cart through the Zod and mutation flow.
            <form onSubmit={checkout} className="cart-layout">
              {/* Displays editable cart rows. */}
              <div>
                {/* Maps browser cart rows into controls. */}
                {cart.map(item => (
                  // Identifies each row by product GUID.
                  <article className="cart-row" key={item.product.productId}>
                    {/* Shows product name and configured API unit price. */}
                    <div>
                      <strong>{item.product.productName}</strong>
                      <small>${(item.product.defaultPrice?.price ?? 0).toFixed(2)} per {item.product.defaultPrice?.unit ?? 'unit'}</small>
                    </div>
                    {/* Allows quantity changes or removal at zero. */}
                    <input
                      aria-label={`Quantity for ${item.product.productName}`}
                      type="number"
                      min="0"
                      value={item.quantity}
                      onChange={event => updateQuantity(item.product.productId, Number(event.target.value))}
                    />
                  </article>
                ))}
              </div>
              {/* Displays memoized checkout summary and submit state. */}
              <aside className="summary">
                {/* Names the order summary. */}
                <h3>Order summary</h3>
                {/* Displays the memoized total. */}
                <strong>${cartTotal.toFixed(2)}</strong>
                {/* Prevents duplicate submissions while the mutation is pending. */}
                <button type="submit" disabled={orderMutation.isPending}>
                  {orderMutation.isPending ? 'Placing order...' : 'Place order'}
                </button>
              </aside>
            </form>
          )}
        </section>
      )}

      {/* Renders location selection when locations is selected. */}
      {tab === 'locations' && (
        // Groups the cascading location selectors.
        <section>
          {/* Names the location page. */}
          <h2>Delivery location</h2>
          {/* Arranges province, city, and barangay controls responsively. */}
          <div className="location-form">
            {/* Provides the province selector. */}
            <label>
              {/* Names the province field. */}
              Province
              {/* Resets city selection whenever province changes. */}
              <select value={provinceId} onChange={event => {
                // Stores the selected province identifier.
                setProvinceId(event.target.value)
                // Clears stale city data and selection.
                setCityId('')
              }}>
                {/* Provides an explicit empty selection. */}
                <option value="">Select province</option>
                {/* Maps API province data into options. */}
                {provincesQuery.data?.items.map(province => (
                  // Uses the stable province identifier as the option key and value.
                  <option key={province.provinceId} value={province.provinceId}>{province.provinceName}</option>
                ))}
              </select>
            </label>
            {/* Provides the city selector. */}
            <label>
              {/* Names the city field. */}
              City
              {/* Disables city selection until a province is chosen. */}
              <select value={cityId} onChange={event => setCityId(event.target.value)} disabled={!provinceId}>
                {/* Provides an explicit empty city selection. */}
                <option value="">Select city</option>
                {/* Maps cached city data into options. */}
                {citiesQuery.data?.items.map(city => (
                  // Uses the city identifier as the option key and value.
                  <option key={city.cityId} value={city.cityId}>{city.cityName}</option>
                ))}
              </select>
            </label>
            {/* Provides the barangay selector. */}
            <label>
              {/* Names the barangay field. */}
              Barangay
              {/* Disables barangay selection until a city is chosen. */}
              <select disabled={!cityId} defaultValue="">
                {/* Provides an explicit empty barangay selection. */}
                <option value="">Select barangay</option>
                {/* Maps cached barangay data into options. */}
                {barangaysQuery.data?.items.map(barangay => (
                  // Uses the compound response identifier as the option key and value.
                  <option key={barangay.barangayId} value={barangay.barangayId}>{barangay.barangayName}</option>
                ))}
              </select>
            </label>
          </div>
        </section>
      )}
    {/* Closes the top-level React container. */}
    </main>
  )
}
