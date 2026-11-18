import React, { useState } from "react";
import { Menu, X } from "lucide-react";
import CategoryList from "@/components/store/CategoryList";

export default function CategoryDrawer({ active, onSelect }) {
  const [open, setOpen] = useState(false);

  const handleSelect = (name) => {
    onSelect(name);
    setOpen(false);
  };

  return (
    <div className="lg:hidden">
      <button
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-2 rounded-full bg-emerald-900 px-4 py-2 text-xs font-medium text-emerald-50"
      >
        <Menu className="h-4 w-4" />
        {active}
      </button>

      {open && (
        <div className="fixed inset-0 z-50">
          <div
            className="absolute inset-0 bg-emerald-950/40"
            onClick={() => setOpen(false)}
          />
          <div className="absolute left-0 top-0 h-full w-72 max-w-[80%] bg-stone-50 p-5 shadow-2xl">
            <div className="mb-4 flex items-center justify-between">
              <p className="text-[10px] uppercase tracking-[0.25em] text-emerald-900/40">Categories</p>
              <button
                onClick={() => setOpen(false)}
                className="rounded-full p-1.5 text-emerald-900/60 hover:bg-emerald-900/5"
                aria-label="Close categories"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
            <CategoryList active={active} onSelect={handleSelect} vertical />
          </div>
        </div>
      )}
    </div>
  );
}