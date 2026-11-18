import React, { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Image } from "@/components/ui/image";
import { Button } from "@/components/ui/button";
import { Minus, Plus, MapPin } from "lucide-react";

export default function ProductDetail({ product, open, onOpenChange, onAdd }) {
  const prices = product?.unit_prices?.length
    ? product.unit_prices
    : product?.price
    ? [{ unit: product.unit || "unit", price: product.price }]
    : [];
  const [selIdx, setSelIdx] = useState(0);
  const [qty, setQty] = useState(1);

  useEffect(() => {
    setSelIdx(0);
    setQty(1);
  }, [product?.id]);

  const selected = prices[selIdx];
  const total = selected ? selected.price * qty : 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="font-display text-xl text-emerald-950">{product?.name}</DialogTitle>
        </DialogHeader>
        {product && (
          <div className="space-y-4">
            <div className="aspect-[4/3] overflow-hidden rounded-2xl bg-stone-100">
              {product.image_url && (
                <Image src={product.image_url} alt={product.name} className="h-full w-full object-cover" />
              )}
            </div>
            <p className="text-sm leading-relaxed text-emerald-900/60">{product.description}</p>
            {product.area_availability?.length > 0 && (
              <div className="flex flex-wrap items-center gap-1.5">
                <MapPin className="h-3.5 w-3.5 text-emerald-900/40" />
                {product.area_availability.map((a) => (
                  <span key={a} className="rounded-full bg-emerald-900/5 px-2.5 py-1 text-[11px] text-emerald-900/70">
                    {a}
                  </span>
                ))}
              </div>
            )}
            <div>
              <p className="mb-2 text-[11px] uppercase tracking-widest text-emerald-900/50">Choose unit</p>
              <div className="space-y-2">
                {prices.map((p, idx) => (
                  <button
                    key={idx}
                    onClick={() => setSelIdx(idx)}
                    className={`flex w-full items-center justify-between rounded-xl px-4 py-3 text-sm transition ${
                      idx === selIdx
                        ? "bg-emerald-900 text-emerald-50"
                        : "bg-white text-emerald-950 ring-1 ring-emerald-900/10 hover:bg-emerald-900/5"
                    }`}
                  >
                    <span>{p.unit}</span>
                    <span className="font-medium">₱{p.price.toLocaleString()}</span>
                  </button>
                ))}
              </div>
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <button
                  onClick={() => setQty((q) => Math.max(1, q - 1))}
                  className="rounded-full p-2 ring-1 ring-emerald-900/15 hover:bg-emerald-900/5"
                >
                  <Minus className="h-4 w-4" />
                </button>
                <span className="w-6 text-center text-sm font-medium">{qty}</span>
                <button
                  onClick={() => setQty((q) => q + 1)}
                  className="rounded-full p-2 ring-1 ring-emerald-900/15 hover:bg-emerald-900/5"
                >
                  <Plus className="h-4 w-4" />
                </button>
              </div>
              <div className="text-right">
                <p className="text-[11px] uppercase tracking-widest text-emerald-900/50">Total</p>
                <p className="font-display text-xl text-emerald-950">₱{total.toLocaleString()}</p>
              </div>
            </div>
            <Button
              className="h-12 w-full font-medium"
              onClick={() => selected && onAdd(product, selected.unit, selected.price, qty)}
            >
              Add to Cart
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}