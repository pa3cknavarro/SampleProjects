import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { base44 } from "@/api/base44Client";
import { ArrowLeft, Leaf } from "lucide-react";

const STATUS_COLOR = {
  Pending: "bg-amber-100 text-amber-700",
  Processing: "bg-blue-100 text-blue-700",
  Shipped: "bg-indigo-100 text-indigo-700",
  Delivered: "bg-stone-200 text-ink",
  Cancelled: "bg-red-100 text-red-700",
};

export default function OrderDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const all = await base44.entities.Order.list("-created_date", 200);
        const o = all.find((x) => x.id === id);
        setOrder(o || null);
      } catch (e) {
        setOrder(null);
      }
      setLoading(false);
    })();
  }, [id]);

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-stone-50">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-900/10 border-t-emerald-900" />
      </div>
    );
  }

  if (!order) {
    return (
      <div className="min-h-screen bg-stone-50">
        <header className="sticky top-0 z-30 border-b border-emerald-900/10 bg-stone-50/80 backdrop-blur-xl">
          <div className="mx-auto flex max-w-3xl items-center justify-between px-5 py-4">
            <button
              onClick={() => navigate("/store")}
              className="inline-flex items-center gap-1.5 rounded-full px-3 py-2 text-xs font-medium text-emerald-900/70 ring-1 ring-emerald-900/15 transition hover:bg-emerald-900/5"
            >
              <ArrowLeft className="h-4 w-4" /> Back to store
            </button>
          </div>
        </header>
        <main className="mx-auto max-w-3xl px-5 py-16 text-center">
          <p className="text-sm text-emerald-900/50">Order not found.</p>
        </main>
      </div>
    );
  }

  const items = Array.isArray(order.items) ? order.items : [];

  return (
    <div className="min-h-screen bg-stone-50">
      <header className="sticky top-0 z-30 border-b border-emerald-900/10 bg-stone-50/80 backdrop-blur-xl">
        <div className="mx-auto flex max-w-3xl items-center justify-between px-5 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-900">
              <Leaf className="h-4 w-4 text-emerald-50" />
            </div>
            <p className="font-display text-lg text-emerald-950">Order Details</p>
          </div>
          <button
            onClick={() => navigate("/store")}
            className="inline-flex items-center gap-1.5 rounded-full px-3 py-2 text-xs font-medium text-emerald-900/70 ring-1 ring-emerald-900/15 transition hover:bg-emerald-900/5"
          >
            <ArrowLeft className="h-4 w-4" /> Back to store
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-5 py-8">
        <div className="rounded-2xl bg-white p-6 ring-1 ring-emerald-900/10">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="font-display text-2xl text-emerald-950">{order.order_number}</h1>
              <p className="text-sm text-emerald-900/50">{order.order_date || "—"}</p>
            </div>
            <span
              className={`rounded-full px-3 py-1 text-xs font-medium ${
                STATUS_COLOR[order.status] || "bg-stone-100 text-stone-700"
              }`}
            >
              {order.status || "Pending"}
            </span>
          </div>

          <div className="mt-4 grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-emerald-900/50">Customer</p>
              <p className="text-emerald-950">{order.customer || "—"}</p>
            </div>
            <div>
              <p className="text-emerald-900/50">Total</p>
              <p className="font-medium text-emerald-950">₱{(order.total || 0).toLocaleString()}</p>
            </div>
          </div>

          <div className="mt-6">
            <p className="mb-2 text-[11px] uppercase tracking-widest text-emerald-900/50">Items</p>
            <div className="divide-y divide-emerald-900/5">
              {items.length ? (
                items.map((it, i) => (
                  <div key={i} className="flex justify-between gap-4 py-3 text-sm">
                    <span className="min-w-0 flex-1 text-emerald-900/70">
                      {it.quantity}× {it.product_name}{" "}
                      {it.unit && <span className="text-emerald-900/40">({it.unit})</span>}
                    </span>
                    <span className="shrink-0 text-emerald-950">
                      ₱{((it.price || 0) * (it.quantity || 0)).toLocaleString()}
                    </span>
                  </div>
                ))
              ) : (
                <p className="py-3 text-sm text-emerald-900/40">No items</p>
              )}
            </div>
            <div className="mt-4 flex justify-between border-t border-emerald-900/10 pt-4">
              <span className="font-medium text-emerald-950">Total</span>
              <span className="font-medium text-emerald-950">₱{(order.total || 0).toLocaleString()}</span>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}