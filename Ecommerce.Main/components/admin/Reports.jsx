import React, { useEffect, useState } from "react";
import { base44 } from "@/api/base44Client";

export default function Reports() {
  const [rows, setRows] = useState({ byClass: [], byCategory: [], byRole: [] });

  useEffect(() => {
    (async () => {
      try {
        const [customers, products, users] = await Promise.all([
          base44.entities.Customer.list("-created_date", 500),
          base44.entities.Product.list("-created_date", 500),
          base44.entities.AppUser.list("-created_date", 500),
        ]);
        const classes = ["Wholesale", "Retail", "VIP"];
        const cats = ["Animal Feeds", "Seeds", "Fertilizers", "Farm Tools", "Pet Food", "Pet Care"];
        const roles = ["User", "Agent", "Admin", "Super Admin"];
        setRows({
          byClass: classes.map((c) => ({ label: c, count: customers.filter((x) => x.class === c).length })),
          byCategory: cats.map((c) => ({ label: c, count: products.filter((x) => x.category === c).length })),
          byRole: roles.map((r) => ({ label: r, count: users.filter((x) => x.role === r).length })),
        });
      } catch (e) {}
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
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Block title="Customers by Class" data={rows.byClass} />
        <Block title="Products by Category" data={rows.byCategory} />
        <Block title="Users by Role" data={rows.byRole} />
      </div>
    </div>
  );
}