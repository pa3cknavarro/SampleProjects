import React, { useEffect, useState } from "react";
import { ecommerceApi } from "@/api";

export default function Reports() {
  const [rows, setRows] = useState({ byClass: [], byCategory: [], byRole: [] });
  const [error, setError] = useState("");

  useEffect(() => {
    (async () => {
      try {
        const [customers, products, users, classes, categories] = await Promise.all([
          ecommerceApi.admin.list("Customer", 500),
          ecommerceApi.admin.list("Product", 500),
          ecommerceApi.admin.list("AppUser", 500),
          ecommerceApi.admin.list("CustomerClass", 500),
          ecommerceApi.admin.list("Category", 500),
        ]);
        setRows({
          byClass: classes.items.map((item) => ({
            label: item.className,
            count: customers.items.filter((customer) => customer.customerClassId === item.customerClassId).length,
          })),
          byCategory: categories.items.map((item) => ({
            label: item.categoryName,
            count: products.items.filter((product) => product.categoryId === item.categoryId).length,
          })),
          byRole: ["User", "Staff", "Admin"].map((role) => ({
            label: role,
            count: users.items.filter((user) => user.role === role).length,
          })),
        });
        setError("");
      } catch (err) {
        setError(err.message || "Could not load report data.");
      }
    })();
  }, []);

  const Block = ({ title, data }) => (
    <div className="rounded-2xl bg-white p-5 ring-1 ring-emerald-900/10">
      <p className="mb-3 text-sm font-medium text-emerald-950">{title}</p>
      <table className="w-full text-sm">
        <tbody>
          {data.map((r) => (
            <tr key={r.label} className="border-b border-emerald-900/5 last:border-0">
              <td className="py-2.5 text-emerald-900/70">{r.label}</td>
              <td className="py-2.5 text-right font-medium text-emerald-950">{r.count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  return (
    <div>
      <h2 className="mb-1 font-display text-2xl text-emerald-950">Reports</h2>
      <p className="mb-5 text-xs text-emerald-900/50">Breakdown of your records</p>
      {error && <p className="mb-4 text-sm text-destructive" role="alert">{error}</p>}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Block title="Customers by Class" data={rows.byClass} />
        <Block title="Products by Category" data={rows.byCategory} />
        <Block title="Users by Role" data={rows.byRole} />
      </div>
    </div>
  );
}