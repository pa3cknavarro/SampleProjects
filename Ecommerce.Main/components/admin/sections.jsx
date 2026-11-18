export const AREAS = ["Baguio", "Bataan", "Bulacan", "La Union", "Nueva Ecija", "Pampanga", "Tarlac"];

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
          { key: "name", label: "Company Name" },
          { key: "class", label: "Class" },
          { key: "contact_person", label: "Contact" },
          { key: "phone", label: "Phone" },
          { key: "province", label: "Province" },
          { key: "city", label: "City" },
          { key: "barangay", label: "Barangay" },
        ],
        fields: [
          { key: "name", label: "Company Name", type: "text", required: true },
          { key: "class", label: "Class", type: "select", optionsEntity: { entity: "CustomerClass", labelKey: "name" } },
          { key: "contact_person", label: "Contact Person", type: "text" },
          { key: "email", label: "Email", type: "email" },
          { key: "phone", label: "Phone", type: "text" },
          { key: "province", label: "Province", type: "select", optionsEntity: { entity: "Province", labelKey: "name" } },
          { key: "city", label: "City", type: "select", optionsEntity: { entity: "City", labelKey: "name" }, dependsOn: ["province"] },
          { key: "barangay", label: "Barangay", type: "select", optionsEntity: { entity: "Barangay", labelKey: "name" }, dependsOn: ["province", "city"] },
          { key: "address", label: "Address", type: "textarea" },
        ],
      },
      {
        key: "class", label: "Class", entity: "CustomerClass", title: "Customer Classes",
        columns: [
          { key: "name", label: "Name" },
          { key: "description", label: "Description" },
        ],
        fields: [
          { key: "name", label: "Name", type: "text", required: true },
          { key: "description", label: "Description", type: "textarea" },
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
          { key: "name", label: "Name" },
          { key: "category", label: "Category" },
          { key: "unit_prices", label: "Price", render: (v) => (v?.length ? "₱" + Math.min(...v.map((p) => p.price)).toLocaleString() + "+" : "—") },
          { key: "area_availability", label: "Areas", render: (v) => (v?.length ? v.length + " area(s)" : "—") },
          { key: "in_stock", label: "In Stock", render: (v) => (v ? "Yes" : "No") },
        ],
        fields: [
          { key: "name", label: "Name", type: "text", required: true },
          { key: "description", label: "Description", type: "textarea" },
          { key: "category", label: "Category", type: "select", options: ["Animal Feeds", "Seeds", "Fertilizers", "Farm Tools", "Pet Food", "Pet Care"] },
          { key: "unit_prices", label: "Unit Prices", type: "priceList", optionsEntity: { entity: "UnitOfMeasure", labelKey: "name" } },
          { key: "area_availability", label: "Area Availability", type: "multiselect", options: AREAS },
          { key: "image_url", label: "Image URL", type: "text" },
          { key: "in_stock", label: "In Stock", type: "checkbox" },
        ],
      },
      {
        key: "categories", label: "Categories", entity: "Category", title: "Categories",
        columns: [
          { key: "name", label: "Name" },
          { key: "description", label: "Description" },
        ],
        fields: [
          { key: "name", label: "Name", type: "text", required: true },
          { key: "description", label: "Description", type: "textarea" },
        ],
      },
      {
        key: "uom", label: "Units of Measure", entity: "UnitOfMeasure", title: "Units of Measure",
        columns: [
          { key: "name", label: "Name" },
          { key: "abbreviation", label: "Abbreviation" },
        ],
        fields: [
          { key: "name", label: "Name", type: "text", required: true },
          { key: "abbreviation", label: "Abbreviation", type: "text" },
        ],
      },
    ],
  },
  {
    group: "Sales",
    items: [
      {
        key: "orders", label: "Orders", entity: "Order", title: "Orders",
        columns: [
          { key: "order_number", label: "Order #" },
          { key: "customer", label: "Customer" },
          { key: "order_date", label: "Date" },
          { key: "status", label: "Status" },
          { key: "total", label: "Total", render: (v) => "₱" + (v || 0).toLocaleString() },
        ],
        fields: [
          { key: "order_number", label: "Order #", type: "text", required: true },
          { key: "customer", label: "Customer", type: "select", optionsEntity: { entity: "Customer", labelKey: "name" }, required: true },
          { key: "order_date", label: "Order Date", type: "date" },
          { key: "status", label: "Status", type: "select", options: ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"] },
          { key: "total", label: "Total", type: "number" },
          { key: "items", label: "Items (one per line: name, unit, qty, price)", type: "textarea" },
        ],
      },
    ],
  },
  {
    group: "Location",
    items: [
      {
        key: "province", label: "Province", entity: "Province", title: "Provinces",
        columns: [
          { key: "name", label: "Name" },
          { key: "region", label: "Region" },
        ],
        fields: [
          { key: "name", label: "Name", type: "text", required: true },
          { key: "region", label: "Region", type: "text" },
        ],
      },
      {
        key: "cities", label: "Cities", entity: "City", title: "Cities",
        columns: [
          { key: "name", label: "Name" },
          { key: "province", label: "Province" },
        ],
        fields: [
          { key: "name", label: "Name", type: "text", required: true },
          { key: "province", label: "Province", type: "select", optionsEntity: { entity: "Province", labelKey: "name" } },
        ],
      },
      {
        key: "barangays", label: "Barangays", entity: "Barangay", title: "Barangays",
        columns: [
          { key: "name", label: "Barangay" },
          { key: "city", label: "City" },
          { key: "province", label: "Province" },
        ],
        fields: [
          { key: "province", label: "Province", type: "select", optionsEntity: { entity: "Province", labelKey: "name" }, required: true },
          { key: "city", label: "City", type: "select", optionsEntity: { entity: "City", labelKey: "name" }, dependsOn: ["province"], required: true },
          { key: "name", label: "Barangay", type: "text", required: true },
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
          { key: "company_name", label: "Company Name" },
          { key: "user_name", label: "Username" },
          { key: "role", label: "Role" },
          { key: "disable", label: "Disabled", render: (v) => (v ? "Yes" : "No") },
        ],
        fields: [
          { key: "company_name", label: "Company Name", type: "select", optionsEntity: { entity: "Customer", labelKey: "name" } },
          { key: "user_name", label: "Username", type: "text", required: true },
          { key: "password", label: "Password", type: "password", required: true },
          { key: "confirm_password", label: "Confirm Password", type: "password", required: true, submit: false },
          { key: "role", label: "Role", type: "select", options: ["User", "Agent", "Admin", "Super Admin"], required: true },
          { key: "disable", label: "Disable", type: "checkbox" },
        ],
      },
    ],
  },
];