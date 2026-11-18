import React, { useEffect, useState } from "react";
import { ecommerceApi } from "@/api";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";
import { Package, Users, MapPin, UserCheck } from "lucide-react";

export default function Dashboard() {
  const [stats, setStats] = useState({ products: 0, customers: 0, users: 0, provinces: 0 });
  const [byCategory, setByCategory] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    (async () => {
      try {
        const [products, customers, appUsers, provinces, categories] = await Promise.all([
          ecommerceApi.admin.list("Product", 500),
          ecommerceApi.admin.list("Customer", 500),
          ecommerceApi.admin.list("AppUser", 500),
          ecommerceApi.admin.list("Province", 500),
          ecommerceApi.admin.list("Category", 500),
        ]);
        setStats({
          products: products.totalCount,
          customers: customers.totalCount,
          users: appUsers.totalCount,
          provinces: provinces.totalCount,
        });
        setByCategory(categories.items.map((category) => ({
          name: category.categoryName,
          count: products.items.filter((product) => product.categoryId === category.categoryId).length,
        })));
        setError("");
      } catch (err) {
        setError(err.message || "Could not load dashboard data.");
      }
    })();
  }, []);

  const cards = [
    { label: "Products", value: stats.products ?? 0, icon: Package },
    { label: "Customers", value: stats.customers ?? 0, icon: Users },
    { label: "System Users", value: stats.users ?? 0, icon: UserCheck },
    { label: "Provinces", value: stats.provinces ?? 0, icon: MapPin },
  ];

  return (
    <div>
      <h2 className="mb-1 font-display text-2xl text-emerald-950">Dashboard</h2>
      <p className="mb-5 text-xs text-emerald-900/50">Overview of your store</p>
      {error && <p className="mb-4 text-sm text-destructive" role="alert">{error}</p>}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map((c) => {
          const Icon = c.icon;
          return (
            <div key={c.label} className="rounded-2xl bg-white p-5 ring-1 ring-emerald-900/10">
              <div className="flex items-center justify-between">
                <p className="text-[11px] uppercase tracking-widest text-emerald-900/50">{c.label}</p>
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-900/5">
                  <Icon className="h-4 w-4 text-emerald-900/60" />
                </div>
              </div>
              <p className="mt-3 font-display text-3xl text-emerald-950">{c.value}</p>
            </div>
          );
        })}
      </div>
      <div className="mt-6 rounded-2xl bg-white p-5 ring-1 ring-emerald-900/10">
        <p className="mb-4 text-sm font-medium text-emerald-950">Products by Category</p>
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={byCategory}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e7e5e4" />
              <XAxis dataKey="name" tick={{ fontSize: 11, fill: "#0f3d32" }} interval={0} angle={-15} textAnchor="end" height={50} />
              <YAxis tick={{ fontSize: 11, fill: "#0f3d32" }} allowDecimals={false} />
              <Tooltip cursor={{ fill: "#0f3d3210" }} />
              <Bar dataKey="count" fill="#0f3d32" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}