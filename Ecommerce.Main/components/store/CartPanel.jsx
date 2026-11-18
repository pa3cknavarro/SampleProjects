import React from "react";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Minus, Plus, Trash2, ShoppingBag } from "lucide-react";
import { Image } from "@/components/ui/image";

export default function CartPanel({ open, onOpenChange, items, onQty, onRemove, onCheckout }) {
  const subtotal = items.reduce((s, i) => s + i.price * i.quantity, 0);
  const shipping = subtotal === 0 ? 0 : subtotal >= 3000 ? 0 : 150;
  const discountRate = 0.10;
  const discount = subtotal === 0 ? 0 : Math.round(subtotal * discountRate);
  const total = subtotal + shipping - discount;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="flex w-full flex-col bg-stone-50 sm:max-w-md">
        <SheetHeader>
          <SheetTitle className="font-display text-2xl text-emerald-950">Your Cart</SheetTitle>
        </SheetHeader>

        {items.length === 0 ? (
          <div className="flex flex-1 flex-col items-center justify-center gap-3 text-emerald-900/40">
            <ShoppingBag className="w-10 h-10" />
            <p className="text-sm">Your cart is empty</p>
          </div>
        ) : (
          <div className="-mx-2 flex-1 space-y-3 overflow-y-auto px-2 py-4">
            {items.map((item) => (
              <div key={item.id} className="flex gap-4 rounded-xl bg-white p-3 ring-1 ring-emerald-900/10">
                <div className="h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-stone-100">
                  {item.image_url && <Image src={item.image_url} alt={item.product_name} className="h-full w-full object-cover" />}
                </div>
                <div className="flex-1">
                  <p className="text-sm text-emerald-950">{item.product_name}</p>
                  {item.unit && (
                    <p className="text-[11px] uppercase tracking-widest text-emerald-900/40">per {item.unit}</p>
                  )}
                  <p className="text-sm text-emerald-900/50">₱{item.price?.toLocaleString()}</p>
                  <div className="mt-2 flex items-center gap-3">
                    <button onClick={() => onQty(item, -1)} className="rounded-full p-1 ring-1 ring-emerald-900/15 hover:bg-emerald-900/5">
                      <Minus className="w-3 h-3" />
                    </button>
                    <span className="w-4 text-center text-sm">{item.quantity}</span>
                    <button onClick={() => onQty(item, 1)} className="rounded-full p-1 ring-1 ring-emerald-900/15 hover:bg-emerald-900/5">
                      <Plus className="w-3 h-3" />
                    </button>
                    <button onClick={() => onRemove(item)} className="ml-auto text-emerald-900/40 hover:text-red-600">
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="space-y-2 border-t border-emerald-900/10 pt-4">
          <div className="flex items-center justify-between text-sm">
            <span className="text-emerald-900/60">Subtotal</span>
            <span className="text-emerald-950">₱{subtotal.toLocaleString()}</span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-emerald-900/60">Shipping</span>
            <span className="text-emerald-950">
              {shipping === 0 ? (subtotal > 0 ? "Free" : "—") : "₱" + shipping.toLocaleString()}
            </span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-emerald-900/60">Discount (10%)</span>
            <span className="text-emerald-600">−₱{discount.toLocaleString()}</span>
          </div>
          <div className="flex items-center justify-between pt-1">
            <span className="text-xs uppercase tracking-widest text-emerald-900/50">Total</span>
            <span className="font-display text-2xl text-emerald-950">₱{total.toLocaleString()}</span>
          </div>
          <button
            onClick={onCheckout}
            disabled={items.length === 0}
            className="mt-3 w-full rounded-full bg-emerald-900 px-5 py-3 text-sm font-medium tracking-wide text-emerald-50 transition hover:bg-emerald-800 disabled:cursor-not-allowed disabled:opacity-40"
          >
            Checkout
          </button>
        </div>
      </SheetContent>
    </Sheet>
  );
}