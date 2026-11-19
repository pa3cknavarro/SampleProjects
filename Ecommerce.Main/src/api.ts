// Describes a backend collection response so pagination and item types stay consistent.
export interface PaginatedResponse<T> {
  // Stores the returned collection for UI rendering.
  items: T[];
  // Reports the total number of matching records.
  totalCount: number;
  // Identifies the currently requested page.
  page: number;
  // Identifies the number of records requested per page.
  pageSize: number;
  // Reports the number of pages available in the backend response.
  totalPages: number;
}

// Models the category fields returned by the API.
export interface Category {
  // Uniquely identifies the category.
  categoryId: string;
  // Provides the category display name.
  categoryName: string;
}

// Models the product fields consumed by the storefront.
export interface Product {
  // Uniquely identifies the product.
  productId: string;
  // Provides the product display name.
  productName: string;
  // Provides the product description.
  productDescription: string;
  // Associates the product with its category.
  categoryId: string;
  // Indicates whether the API allows the product to be added.
  inStock: boolean;
  defaultPrice?: { uomId: string; unit: string; price: number } | null;
}

// Models a province returned by the location API.
export interface Province {
  // Uniquely identifies the province.
  provinceId: string;
  // Provides the province display name.
  provinceName: string;
}

// Models a city and its parent province.
export interface City {
  // Associates the city with its province.
  provinceId: string;
  // Uniquely identifies the city.
  cityId: string;
  // Provides the city display name.
  cityName: string;
  // Provides the postal code.
  zipCode: string;
}

// Models a barangay and both parent identifiers.
export interface Barangay {
  // Associates the barangay with its province.
  provinceId: string;
  // Associates the barangay with its city.
  cityId: string;
  // Uniquely identifies the barangay.
  barangayId: string;
  // Provides the barangay display name.
  barangayName: string;
}

export interface AuthUser {
  id: string;
  companyId: string;
  customerId: string | null;
  username: string;
  email: string;
  role: 'User' | 'Staff' | 'Admin';
  isActive: boolean;
}

export interface CustomerProfile {
  customerId: string;
  companyName: string;
  contactPerson: string;
  emailAddress: string;
  contactNumber: string;
  customerClass?: { className: string };
  province?: { provinceName: string } | null;
  city?: { cityName: string } | null;
  barangay?: { barangayName: string } | null;
  address: string;
  provinceId?: string | null;
}

export interface Order {
  orderId: string;
  orderDate: string;
  customerId: string;
  shippingProvinceId: string | null;
  orderStatus: number;
  grandTotal: number;
  orderDetail: Array<{
    productId: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    uomId: string;
    unit: string;
  }>;
}

// Normalizes backend and network error information for React UI consumers.
export interface ApiError {
  // Stores the HTTP or synthetic network status.
  status: number;
  // Stores a short human-readable error title.
  title: string;
  // Stores the safe error detail shown to users.
  detail: string;
}

// Converts an API error envelope into a normal JavaScript Error.
export class EcommerceApiError extends Error {
  // Preserves the structured API error while initializing the base error message.
  constructor(public readonly apiError: ApiError) {
    // Supplies the API detail as the conventional Error message.
    super(apiError.detail);
  }
}

// Uses a same-origin proxy in development so the demo API key is never sent in browser JavaScript.
const API_BASE_URL = (import.meta.env.VITE_ECOMMERCE_API_URL ?? '/api/v1').replace(/\/$/, '');
const ADMIN_ENTITY_ROUTES: Record<string, { route: string; id: string; canCreate?: boolean; canDelete?: boolean }> = {
  Customer: { route: 'customers', id: 'customerId' },
  CustomerClass: { route: 'customer-classes', id: 'customerClassId' },
  Product: { route: 'products', id: 'productId' },
  UnitPrice: { route: 'unit-prices', id: 'unitPriceId' },
  AreaAvailable: { route: 'areas-available', id: 'areaAvailableId' },
  Category: { route: 'categories', id: 'categoryId' },
  UnitOfMeasure: { route: 'unit-of-measures', id: 'id' },
  Order: { route: 'orders', id: 'orderId', canCreate: false, canDelete: false },
  Province: { route: 'provinces', id: 'provinceId' },
  City: { route: 'cities', id: 'cityId' },
  Barangay: { route: 'barangays', id: 'barangayId' },
  AppUser: { route: 'users', id: 'id' },
};

function getAdminEntityConfig(entity: string) {
  const config = ADMIN_ENTITY_ROUTES[entity];
  if (!config) throw new Error(`Unsupported admin entity: ${entity}`);
  return config;
}

function getAdminEntityId(entity: string, row: Record<string, unknown>) {
  if (entity === 'City') return `${row.provinceId}/${row.cityId}`;
  if (entity === 'Barangay') return `${row.provinceId}/${row.cityId}/${row.barangayId}`;
  if (entity === 'UnitPrice') return `${row.productId}/${row.uomId}/${row.unitPriceId}`;
  const id = row[getAdminEntityConfig(entity).id];
  if (typeof id !== 'string') throw new Error(`Missing identifier for admin entity: ${entity}`);
  return id;
}

async function request<T>(path: string, init?: RequestInit, timeoutMs = 15_000): Promise<T> {
  // Combines caller cancellation with a bounded request timeout.
  const timeout = new AbortController();
  // Captures a caller-provided cancellation signal when supplied.
  const callerSignal = init?.signal;
  // Defines one abort handler so timeout and caller cancellation can share behavior.
  const abortFromCaller = () => timeout.abort();
  // Relays caller cancellation into the centralized request controller.
  callerSignal?.addEventListener('abort', abortFromCaller, { once: true });
  // Starts a bounded timeout to prevent a hung network request from waiting indefinitely.
  const timer = window.setTimeout(() => timeout.abort(), timeoutMs);
  // Holds the fetch result outside the try block so status handling can follow it.
  let response: Response;
  try {
    // Sends the JSON request with centralized serialization and abort handling.
    response = await fetch(path, {
      // Preserves caller-provided method, body, and request options.
      ...init,
      // Applies the shared abort signal to support both timeout and caller cancellation.
      signal: timeout.signal,
      // Supplies JSON headers while allowing feature calls to add extra headers.
      headers: {
        // Declares JSON serialization for request bodies.
        'Content-Type': 'application/json',
        // Preserves any feature-specific headers supplied by the caller.
        ...init?.headers,
      },
    });
  } catch (cause) {
    // Distinguishes an expected timeout from an ordinary network failure.
    if (timeout.signal.aborted) {
      // Raises a consistent timeout error even though no HTTP response arrived.
      throw new EcommerceApiError({
        // Uses a conventional timeout status for UI branching.
        status: 408,
        // Names the failure as a timeout.
        title: 'Request timeout',
        // Gives the UI an actionable, non-sensitive timeout detail.
        detail: 'The API did not respond before the request timeout.',
      });
    }
    // Raises a normalized network error for connection and transport failures.
    throw new EcommerceApiError({
      // Uses zero because no HTTP status was produced.
      status: 0,
      // Names the failure for UI display.
      title: 'Network error',
      // Gives a consistent detail independent of raw browser exceptions.
      detail: 'Unable to reach the Ecommerce API.',
    });
  } finally {
    // Stops the timeout timer because the request has settled or failed.
    window.clearTimeout(timer);
    // Removes the caller listener to prevent stale references and duplicate abort calls.
    callerSignal?.removeEventListener('abort', abortFromCaller);
  }

  // Handles all unsuccessful HTTP statuses consistently.
  if (!response.ok) {
    // Starts with a status-based fallback if the API returns no JSON envelope.
    let apiError: ApiError = {
      // Preserves the actual HTTP status code.
      status: response.status,
      // Uses the browser reason phrase or a fallback title.
      title: response.statusText || 'Request failed',
      // Provides a safe generic detail that does not expose implementation details.
      detail: `The API returned HTTP ${response.status}.`,
    };
    try {
      // Attempts to replace fallback values with the backend error envelope.
      apiError = await response.json();
    } catch {
      // Retains the status-based fallback when a non-JSON or malformed response arrives.
    }
    // Throws the normalized error for feature hooks and UI catch blocks.
    throw new EcommerceApiError(apiError);
  }

  // Returns an explicit undefined value for successful 204 responses.
  if (response.status === 204) return undefined as T;
  // Deserializes successful JSON payloads into the requested TypeScript contract.
  return response.json() as Promise<T>;
}

// Defines all feature API operations behind one exported service object.
export const ecommerceApi = {
  auth: {
    me: () => request<AuthUser>(`${API_BASE_URL}/auth/me`),
    login: (credentials: { username: string; password: string; rememberMe?: boolean }) =>
      request<AuthUser>(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        body: JSON.stringify(credentials),
      }),
    register: (credentials: { email: string; password: string; companyName: string }) =>
      request<{ message: string; verificationCode?: string }>(`${API_BASE_URL}/auth/register`, {
        method: 'POST',
        body: JSON.stringify(credentials),
      }),
    verifyEmail: (verification: { email: string; code: string }) =>
      request<AuthUser>(`${API_BASE_URL}/auth/verify-email`, {
        method: 'POST',
        body: JSON.stringify(verification),
      }),
    resendVerification: (email: string) =>
      request<{ message: string; verificationCode?: string }>(`${API_BASE_URL}/auth/resend-verification`, {
        method: 'POST',
        body: JSON.stringify({ email }),
      }),
    forgotPassword: (email: string) =>
      request<{ message: string; resetUrl?: string }>(`${API_BASE_URL}/auth/forgot-password`, {
        method: 'POST',
        body: JSON.stringify({ email }),
      }),
    resetPassword: (credentials: { token: string; newPassword: string }) =>
      request<void>(`${API_BASE_URL}/auth/reset-password`, {
        method: 'POST',
        body: JSON.stringify(credentials),
      }),
    changePassword: (credentials: { currentPassword: string; newPassword: string }) =>
      request<void>(`${API_BASE_URL}/auth/change-password`, {
        method: 'POST',
        body: JSON.stringify(credentials),
      }),
    logout: () => request<void>(`${API_BASE_URL}/auth/logout`, { method: 'POST' }),
  },
  notifications: {
    list: () => request<{
      items: Array<{ notificationId: string; title: string; message: string; createdOn: string; readOn: string | null }>;
      unreadCount: number;
    }>(`${API_BASE_URL}/notifications?page=1&pageSize=50`),
    markRead: (notificationId: string) => request<void>(`${API_BASE_URL}/notifications/${notificationId}/read`, { method: 'POST' }),
    markAllRead: () => request<void>(`${API_BASE_URL}/notifications/read-all`, { method: 'POST' }),
  },
  getCustomerProfile: () => request<CustomerProfile>(`${API_BASE_URL}/customers/me`),
  updateCustomerProfile: (profile: { emailAddress: string; contactNumber: string }) =>
    request<CustomerProfile>(`${API_BASE_URL}/customers/me`, {
      method: 'PUT',
      body: JSON.stringify(profile),
    }),
  getOrders: (pageSize = 100) =>
    request<PaginatedResponse<Order>>(`${API_BASE_URL}/orders?page=1&pageSize=${pageSize}`),
  getOrder: (orderId: string) => request<Order>(`${API_BASE_URL}/orders/${orderId}`),
  admin: {
    list: async (entity: string, pageSize = 100) => {
      const { route } = getAdminEntityConfig(entity);
      const batchSize = Math.min(Math.max(pageSize, 1), 100);
      const first = await request<PaginatedResponse<Record<string, unknown>>>(
        `${API_BASE_URL}/${route}?page=1&pageSize=${batchSize}`,
      );
      const items = [...first.items];
      for (let page = 2; page <= first.totalPages; page += 1) {
        const result = await request<PaginatedResponse<Record<string, unknown>>>(
          `${API_BASE_URL}/${route}?page=${page}&pageSize=${batchSize}`,
        );
        items.push(...result.items);
      }
      return { ...first, items };
    },
    create: (entity: string, payload: Record<string, unknown>) => {
      const { route, canCreate } = getAdminEntityConfig(entity);
      if (canCreate === false) throw new Error(`${entity} records cannot be created here.`);
      return request<Record<string, unknown>>(`${API_BASE_URL}/${route}`, {
        method: 'POST',
        body: JSON.stringify(payload),
      });
    },
    update: (entity: string, row: Record<string, unknown>, payload: Record<string, unknown>) => {
      const { route } = getAdminEntityConfig(entity);
      const id = getAdminEntityId(entity, row);
      return request<Record<string, unknown>>(`${API_BASE_URL}/${route}/${id}`, {
        method: 'PUT',
        body: JSON.stringify(payload),
      });
    },
    delete: (entity: string, row: Record<string, unknown>) => {
      const { route, canDelete } = getAdminEntityConfig(entity);
      if (canDelete === false) throw new Error(`${entity} records cannot be deleted here.`);
      const id = getAdminEntityId(entity, row);
      return request<void>(`${API_BASE_URL}/${route}/${id}`, { method: 'DELETE' });
    },
  },
  // Retrieves categories with the backend's default pagination contract.
  getCategories: (pageSize = 100) =>
    // Builds the versioned category route and asks for the requested page size.
    request<PaginatedResponse<Category>>(`${API_BASE_URL}/categories?page=1&pageSize=${pageSize}`),
  // Retrieves products with optional search, category, page, and cancellation inputs.
  getProducts: (search = '', categoryId?: string, page = 1, signal?: AbortSignal, provinceId?: string) => {
    // Creates URL parameters without manually concatenating unescaped values.
    const params = new URLSearchParams({ page: String(page), pageSize: '12' });
    // Adds search only when the user entered a nonempty term.
    if (search) params.set('search', search);
    // Adds a category filter only when the category identifier is present.
    if (categoryId) params.set('categoryId', categoryId);
    if (provinceId) params.set('provinceId', provinceId);
    // Sends the versioned product request and forwards cancellation.
    return request<PaginatedResponse<Product>>(`${API_BASE_URL}/products?${params}`, { signal });
  },
  // Retrieves one product using its GUID route parameter.
  getProduct: (productId: string) =>
    // Sends the detail request through the centralized client.
    request<Product>(`${API_BASE_URL}/products/${productId}`),
  // Retrieves the province list used by the location selector.
  getProvinces: (signal?: AbortSignal) =>
    // Requests a large first page while preserving cancellation support.
    request<PaginatedResponse<Province>>(`${API_BASE_URL}/provinces?page=1&pageSize=100`, { signal }),
  // Retrieves cities filtered by their parent province.
  getCities: (provinceId: string, signal?: AbortSignal) =>
    // Sends the province filter through an escaped URL value.
    request<PaginatedResponse<City>>(`${API_BASE_URL}/cities?page=1&pageSize=100&provinceId=${provinceId}`, { signal }),
  // Retrieves barangays filtered by province and city.
  getBarangays: (provinceId: string, cityId: string, signal?: AbortSignal) =>
    // Sends both compound location identifiers to the backend.
    request<PaginatedResponse<Barangay>>(
      // Builds the filtered barangay route.
      `${API_BASE_URL}/barangays?page=1&pageSize=100&provinceId=${provinceId}&cityId=${cityId}`,
      // Forwards cancellation to the shared request function.
      { signal },
    ),
  // Creates an order through the authoritative business endpoint.
  createOrder: (order: unknown) =>
    // Posts the serialized order and supplies a fresh idempotency key.
    request<unknown>(`${API_BASE_URL}/orders`, {
      // Declares a POST mutation.
      method: 'POST',
      // Serializes the Zod-validated order as JSON.
      body: JSON.stringify(order),
      // Prevents accidental duplicate order submission retries.
      headers: { 'X-Idempotency-Key': crypto.randomUUID() },
    }),
};
