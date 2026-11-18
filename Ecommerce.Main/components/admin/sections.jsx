export const SECTIONS = [
  {
    group: "General",
    items: [
      { key: "dashboard", label: "Dashboard", component: "dashboard", title: "Dashboard" },
      { key: "reports", label: "Reports", component: "reports", title: "Reports" },
    ],
  },
  {
    group: "Customer",
    items: [
      {
        key: "customers", label: "Customers", entity: "Customer", title: "Customers",
        columns: [
          { key: "companyName", label: "Company Name" },
          { key: "customerClassId", label: "Class ID" },
          { key: "contactPerson", label: "Contact" },
          { key: "emailAddress", label: "Email" },
          { key: "contactNumber", label: "Phone" },
        ],
        fields: [
          { key: "companyName", label: "Company Name", type: "text", required: true },
          { key: "customerClassId", label: "Class", type: "select", optionsEntity: { entity: "CustomerClass", labelKey: "className", valueKey: "customerClassId" }, required: true },
          { key: "contactPerson", label: "Contact Person", type: "text" },
          { key: "emailAddress", label: "Email", type: "email" },
          { key: "contactNumber", label: "Phone", type: "text" },
          { key: "address", label: "Address", type: "textarea" },
        ],
      },
      {
        key: "class", label: "Class", entity: "CustomerClass", title: "Customer Classes",
        columns: [
          { key: "className", label: "Name" },
          { key: "description", label: "Description" },
        ],
        fields: [
          { key: "className", label: "Name", type: "text", required: true },
          { key: "description", label: "Description", type: "textarea", required: true },
        ],
      },
    ],
  },
  {
    group: "Items",
    items: [
      {
        key: "products", label: "Products", entity: "Product", title: "Products",
        columns: [
          { key: "productName", label: "Name" },
          { key: "categoryId", label: "Category ID" },
          { key: "inStock", label: "In Stock", render: (value) => (value ? "Yes" : "No") },
        ],
        fields: [
          { key: "productName", label: "Name", type: "text", required: true },
          { key: "productDescription", label: "Description", type: "textarea" },
          { key: "categoryId", label: "Category", type: "select", optionsEntity: { entity: "Category", labelKey: "categoryName", valueKey: "categoryId" }, required: true },
          { key: "productImageUrl", label: "Image URL", type: "text" },
          { key: "inStock", label: "In Stock", type: "checkbox" },
        ],
      },
      {
        key: "categories", label: "Categories", entity: "Category", title: "Categories",
        columns: [{ key: "categoryName", label: "Name" }],
        fields: [{ key: "categoryName", label: "Name", type: "text", required: true }],
      },
      {
        key: "uom", label: "Units of Measure", entity: "UnitOfMeasure", title: "Units of Measure",
        columns: [
          { key: "uom", label: "Unit" },
          { key: "description", label: "Description" },
        ],
        fields: [
          { key: "uom", label: "Unit", type: "text", required: true },
          { key: "description", label: "Description", type: "text", required: true },
        ],
      },
      {
        key: "unitPrices", label: "Unit Prices", entity: "UnitPrice", title: "Unit Prices",
        columns: [
          { key: "productName", label: "Product" },
          { key: "unit", label: "Unit" },
          { key: "price", label: "Price", render: (value) => `₱${Number(value).toFixed(2)}` },
        ],
        fields: [
          { key: "productId", label: "Product", type: "select", optionsEntity: { entity: "Product", labelKey: "productName", valueKey: "productId" }, required: true, readOnlyOnEdit: true },
          { key: "uomId", label: "Unit of Measure", type: "select", optionsEntity: { entity: "UnitOfMeasure", labelKey: "uom", valueKey: "id" }, required: true, readOnlyOnEdit: true },
          { key: "price", label: "Price", type: "number", required: true },
        ],
      },
      {
        key: "availability", label: "Area Availability", entity: "AreaAvailable", title: "Area Availability",
        columns: [
          { key: "productName", label: "Product" },
          { key: "provinceName", label: "Province" },
          { key: "isAvailable", label: "Available", render: (value) => (value ? "Yes" : "No") },
        ],
        fields: [
          { key: "productId", label: "Product", type: "select", optionsEntity: { entity: "Product", labelKey: "productName", valueKey: "productId" }, required: true, readOnlyOnEdit: true },
          { key: "provinceId", label: "Province", type: "select", optionsEntity: { entity: "Province", labelKey: "provinceName", valueKey: "provinceId" }, required: true, readOnlyOnEdit: true },
          { key: "isAvailable", label: "Available", type: "checkbox" },
        ],
      },
    ],
  },
  {
    group: "Sales",
    items: [
      {
        key: "orders", label: "Orders", entity: "Order", title: "Orders", allowCreate: false, allowDelete: false,
        columns: [
          { key: "orderId", label: "Order #" },
          { key: "customerId", label: "Customer ID" },
          { key: "orderDate", label: "Date" },
          { key: "orderStatus", label: "Status", render: (value) => ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"][value] || "Pending" },
          { key: "grandTotal", label: "Total", render: (value) => "₱" + (value || 0).toLocaleString() },
        ],
        fields: [
          { key: "orderStatus", label: "Status", type: "select", options: ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"], required: true },
        ],
      },
    ],
  },
  {
    group: "Location",
    items: [
      {
        key: "province", label: "Province", entity: "Province", title: "Provinces",
        columns: [{ key: "provinceName", label: "Name" }],
        fields: [{ key: "provinceName", label: "Name", type: "text", required: true }],
      },
      {
        key: "cities", label: "Cities", entity: "City", title: "Cities",
        columns: [
          { key: "cityName", label: "Name" },
          { key: "provinceId", label: "Province ID" },
          { key: "zipCode", label: "Postal Code" },
        ],
        fields: [
          { key: "provinceId", label: "Province", type: "select", optionsEntity: { entity: "Province", labelKey: "provinceName", valueKey: "provinceId" }, required: true },
          { key: "cityName", label: "Name", type: "text", required: true },
          { key: "zipCode", label: "Postal Code", type: "text" },
        ],
      },
      {
        key: "barangays", label: "Barangays", entity: "Barangay", title: "Barangays",
        columns: [
          { key: "barangayName", label: "Barangay" },
          { key: "cityId", label: "City ID" },
          { key: "provinceId", label: "Province ID" },
        ],
        fields: [
          { key: "provinceId", label: "Province", type: "select", optionsEntity: { entity: "Province", labelKey: "provinceName", valueKey: "provinceId" }, required: true },
          { key: "cityId", label: "City", type: "select", optionsEntity: { entity: "City", labelKey: "cityName", valueKey: "cityId" }, dependsOn: ["provinceId"], required: true },
          { key: "barangayName", label: "Barangay", type: "text", required: true },
        ],
      },
    ],
  },
  {
    group: "Users",
    items: [
      {
        key: "users", label: "Users", entity: "AppUser", title: "Users",
        columns: [
          { key: "username", label: "Username" },
          { key: "email", label: "Email" },
          { key: "role", label: "Role" },
          { key: "isActive", label: "Active", render: (value) => (value ? "Yes" : "No") },
        ],
        fields: [
          { key: "companyId", label: "Company", type: "select", optionsEntity: { entity: "Customer", labelKey: "companyName", valueKey: "customerId" }, required: true },
          { key: "username", label: "Username", type: "text", required: true },
          { key: "email", label: "Email", type: "email", required: true },
          { key: "password", label: "Password", type: "password" },
          { key: "role", label: "Role", type: "select", options: ["User", "Staff", "Admin"], required: true },
          { key: "isActive", label: "Active", type: "checkbox" },
        ],
      },
    ],
  },
];
