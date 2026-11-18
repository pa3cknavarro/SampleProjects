import React from "react";
import { motion } from "framer-motion";
import { Plus, Check, Flame } from "lucide-react";
import { Image } from "@/components/ui/image";

export default function ProductCard({ product, onAdd, added, bestSeller }) {
  const hasOptions = (product.unit_prices?.length || 0) > 1;
  const fromPrice = product.unit_prices?.length
    ? Math.min(...product.unit_prices.map((p) => p.price))
    : product.price;

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25 }}
      onClick={() => onAdd(product)}
      className="group flex cursor-pointer flex-col overflow-hidden rounded-xl bg-white ring-1 ring-emerald-900/10 transition-all duration-300 hover:ring-emerald-900/25"
    >
      <div className="relative h-28 overflow-hidden bg-stone-100 sm:h-36">
        {product.image_url && (
          <Image src={product.image_url} alt={product.name} className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-105" />
        )}
        <span className="absolute left-1.5 top-1.5 rounded-full bg-white/90 px-1.5 py-0.5 text-[8px] uppercase tracking-widest text-emerald-900 sm:left-2 sm:top-2 sm:px-2 sm:text-[9px]">
          {product.category}
        </span>
        {bestSeller && (
          <span className="absolute right-1.5 top-1.5 inline-flex items-center gap-0.5 rounded-full bg-amber-500 px-1.5 py-0.5 text-[8px] font-semibold uppercase tracking-wider text-white shadow-sm sm:right-2 sm:top-2 sm:gap-1 sm:px-2 sm:text-[9px]">
            <Flame className="h-2 w-2 sm:h-2.5 sm:w-2.5" /> Best Seller
          </span>
        )}
      </div>
      <div className="flex flex-1 flex-col p-2.5 sm:p-4">
        <h3 className="font-heading text-xs leading-snug text-emerald-950 line-clamp-1 sm:text-sm">{product.name}</h3>
        <p className="mt-0.5 line-clamp-2 text-[10px] leading-snug text-emerald-900/55 sm:mt-1 sm:text-xs">{product.description}</p>
        <div className="mt-2 flex items-end justify-between gap-1.5 sm:mt-2.5 sm:gap-2">
          <div className="min-w-0">
            <p className="font-display text-sm leading-none text-emerald-950 sm:text-base">
              {hasOptions ? "₱" + fromPrice.toLocaleString() + "+" : "₱" + (fromPrice?.toLocaleString() ?? 0)}
            </p>
            {product.unit_prices?.length ? (
              <p className="mt-0.5 truncate text-[9px] uppercase tracking-widest text-emerald-900/40 sm:text-[10px]">per {product.unit_prices[0].unit}</p>
            ) : (
              product.unit && <p className="mt-0.5 truncate text-[9px] uppercase tracking-widest text-emerald-900/40 sm:text-[10px]">per {product.unit}</p>
            )}
          </div>
          <button
            onClick={(e) => { e.stopPropagation(); onAdd(product); }}
            className="flex shrink-0 items-center gap-1 rounded-full bg-emerald-900 px-2.5 py-1.5 text-[10px] font-medium tracking-wide text-emerald-50 transition-all duration-300 hover:bg-emerald-800 active:scale-95 sm:gap-1.5 sm:px-3 sm:text-[11px]"
          >
            {added ? <Check className="w-3 h-3 sm:w-3.5 sm:h-3.5" /> : <Plus className="w-3 h-3 sm:w-3.5 sm:h-3.5" />}
            {added ? "Added" : hasOptions ? "Options" : "Add"}
          </button>
        </div>
      </div>
    </motion.div>
  );
}