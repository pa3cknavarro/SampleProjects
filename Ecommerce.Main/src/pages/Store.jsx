import React, { useState, useEffect, useMemo } from "react";
import { base44 } from "@/api/base44Client";
import StoreHeader from "@/components/store/StoreHeader";
import CategoryList from "@/components/store/CategoryList";
import CategoryDrawer from "@/components/store/CategoryDrawer";
import ProductCard from "@/components/store/ProductCard";
import CartPanel from "@/components/store/CartPanel";
import AppBanner from "@/components/store/AppBanner";
import PromoBanner from "@/components/store/PromoBanner";
import ProductDetail from "@/components/store/ProductDetail";
import MobileTabBar from "@/components/store/MobileTabBar";
import { getStorefrontUser, clearStorefrontUser } from "@/lib/storefrontAuth";
import { useNavigate } from "react-router-dom";
import { Search, History, Flame } from "lucide-react";
import { Input } from "@/components/ui/input";

export default function Store() {
  const [user, setUser] = useState(null);
  const [customer, setCustomer] = useState(null);
  const [orders, setOrders] = useState([]);
  const [products, setProducts] = useState([]);
  const [cart, setCart] = useState([]);
  const [category, setCategory] = useState("All Products");
  const [cartOpen, setCartOpen] = useState(false);
  const [addedId, setAddedId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [detailProduct, setDetailProduct] = useState(null);
  const [query, setQuery] = useState("");
  const [allOrders, setAllOrders] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    (async () => {
      const su = getStorefrontUser();
      let prov = null;
      if (su) {
        setUser(su);
        try {
          setCart(await base44.entities.CartItem.filter({ app_user_id: su.id }));
        } catch (e) {}
        if (su.company_name) {
          try {
            const custs = await base44.entities.Customer.filter({ name: su.company_name });
            const cust = custs[0];
            setCustomer(cust);
            prov = cust?.province || null;
            const ords = await base44.entities.Order.filter({ customer: su.company_name });
            setOrders(ords);
          } catch (e) {}
        }
      }
      const all = await base44.entities.Product.list("-created_date", 200);
      const visible = prov
        ? all.filter((p) => !p.area_availability?.length || p.area_availability.includes(prov))
        : all;
      setProducts(visible);
      try {
        const allOrds = await base44.entities.Order.list("-created_date", 500);
        setAllOrders(allOrds);
      } catch (e) {}
      setLoading(false);
    })();
  }, []);

  const login = () => navigate("/login");
  const logout = () => {
    clearStorefrontUser();
    window.location.href = "/";
  };
  const goAdmin = () => navigate("/admin");
  const updateCustomer = (c) => setCustomer(c);

  const openDetail = (product) => setDetailProduct(product);

  const addToCart = async (product, unit, price, qty = 1) => {
    if (!user) {
      setDetailProduct(null);
      return login();
    }
    const existing = cart.find((c) => c.product_id === product.id && c.unit === unit);
    if (existing) {
      const updated = await base44.entities.CartItem.update(existing.id, {
        quantity: existing.quantity + qty,
      });
      setCart(cart.map((c) => (c.id === existing.id ? updated : c)));
    } else {
      const created = await base44.entities.CartItem.create({
        app_user_id: user.id,
        product_id: product.id,
        product_name: product.name,
        price,
        unit,
        image_url: product.image_url,
        quantity: qty,
      });
      setCart([...cart, created]);
    }
    setDetailProduct(null);
    setAddedId(product.id);
    setTimeout(() => setAddedId(null), 1200);
    setCartOpen(true);
  };

  const changeQty = async (item, delta) => {
    const qty = item.quantity + delta;
    if (qty < 1) return removeItem(item);
    const updated = await base44.entities.CartItem.update(item.id, { quantity: qty });
    setCart(cart.map((c) => (c.id === item.id ? updated : c)));
  };

  const removeItem = async (item) => {
    await base44.entities.CartItem.delete(item.id);
    setCart(cart.filter((c) => c.id !== item.id));
  };

  const recentlyBoughtNames = useMemo(() => {
    const names = new Set();
    orders.forEach((o) => o.items?.forEach((i) => i.product_name && names.add(i.product_name)));
    return names;
  }, [orders]);

  const bestSellerNames = useMemo(() => {
    const counts = {};
    allOrders.forEach((o) =>
      o.items?.forEach((i) => {
        if (i.product_name) counts[i.product_name] = (counts[i.product_name] || 0) + (i.quantity || 1);
      })
    );
    return new Set(
      Object.entries(counts)
        .sort((a, b) => b[1] - a[1])
        .slice(0, 10)
        .map(([n]) => n)
    );
  }, [allOrders]);

  const visible = products.filter((p) => {
    if (category === "Recently Bought") {
      if (!recentlyBoughtNames.has(p.name)) return false;
    } else if (category === "Best Sellers") {
      if (!bestSellerNames.has(p.name)) return false;
    } else if (category !== "All Products" && p.category !== category) {
      return false;
    }
    if (!query) return true;
    const q = query.toLowerCase();
    return (p.name || "").toLowerCase().includes(q) || (p.description || "").toLowerCase().includes(q);
  });
  const count = cart.reduce((s, i) => s + i.quantity, 0);

  return (
    <div className="min-h-screen bg-stone-50">
      <AppBanner />
      <StoreHeader
        user={user}
        customer={customer}
        orders={orders}
        count={count}
        onCartClick={() => setCartOpen(true)}
        onLogin={login}
        onLogout={logout}
        onAdmin={goAdmin}
        onCustomerUpdate={updateCustomer}
      />

      <main className="mx-auto max-w-7xl px-5 pb-28 pt-8 lg:px-10 lg:pb-14 lg:pt-14">
        <PromoBanner />

        <div className="mt-6 grid grid-cols-1 gap-8 lg:grid-cols-[220px_1fr] lg:gap-12 lg:mt-10">
          <aside className="lg:sticky lg:top-28 lg:self-start">
            <div className="mb-4 lg:hidden">
              <CategoryDrawer active={category} onSelect={setCategory} />
            </div>
            <p className="mb-3 hidden text-[10px] uppercase tracking-[0.25em] text-emerald-900/40 lg:block">Categories</p>
            <div className="hidden lg:block">
              <CategoryList active={category} onSelect={setCategory} />
            </div>
          </aside>

          <section>
            <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
              <div className="relative flex-1 sm:min-w-[200px] sm:max-w-sm">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-emerald-900/40" />
                <Input
                  placeholder="Search products..."
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  className="pl-9"
                />
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setCategory((c) => (c === "Best Sellers" ? "All Products" : "Best Sellers"))}
                  className={`inline-flex items-center gap-1.5 rounded-full px-3.5 py-2 text-xs font-medium transition ${
                    category === "Best Sellers"
                      ? "bg-amber-500 text-white"
                      : "text-amber-700 ring-1 ring-amber-500/30 hover:bg-amber-500/5"
                  }`}
                >
                  <Flame className="h-3.5 w-3.5" /> Best Sellers
                </button>
                {orders.length > 0 && (
                  <button
                    onClick={() => setCategory((c) => (c === "Recently Bought" ? "All Products" : "Recently Bought"))}
                    className={`inline-flex items-center gap-1.5 rounded-full px-3.5 py-2 text-xs font-medium transition ${
                      category === "Recently Bought"
                        ? "bg-amber-500 text-white"
                        : "text-amber-700 ring-1 ring-amber-500/30 hover:bg-amber-500/5"
                    }`}
                  >
                    <History className="h-3.5 w-3.5" /> Recently Bought
                  </button>
                )}
              </div>
            </div>
            {loading ? (
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-2 xl:grid-cols-3">
                {[0, 1, 2, 3, 4, 5].map((i) => (
                  <div key={i} className="h-44 animate-pulse rounded-xl bg-emerald-900/5 sm:h-72 sm:rounded-2xl" />
                ))}
              </div>
            ) : visible.length === 0 ? (
              <p className="py-12 text-center text-sm text-emerald-900/45 sm:py-20">
                No items available for your area yet.
              </p>
            ) : (
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-2 xl:grid-cols-3">
                {visible.map((p) => (
                  <ProductCard key={p.id} product={p} onAdd={openDetail} added={addedId === p.id} bestSeller={bestSellerNames.has(p.name)} />
                ))}
              </div>
            )}
          </section>
        </div>
      </main>

      <CartPanel
        open={cartOpen}
        onOpenChange={setCartOpen}
        items={cart}
        onQty={changeQty}
        onRemove={removeItem}
        onCheckout={() => {
          setCartOpen(false);
          alert("Checkout coming soon — your order is ready to place.");
        }}
      />

      <ProductDetail
        product={detailProduct}
        open={!!detailProduct}
        onOpenChange={(o) => !o && setDetailProduct(null)}
        onAdd={addToCart}
      />

      {user && (
        <MobileTabBar
          onCart={() => setCartOpen(true)}
          onAdmin={goAdmin}
          onLogout={logout}
          cartCount={count}
          isAdmin={user?.role !== "User"}
        />
      )}
    </div>
  );
}