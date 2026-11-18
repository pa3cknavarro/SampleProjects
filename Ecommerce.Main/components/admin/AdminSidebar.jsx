import React, { useState } from "react";
import { Menu, X } from "lucide-react";

export default function AdminSidebar({ sections, active, onSelect }) {
  const [open, setOpen] = useState(false);

  const choose = (key) => {
    onSelect(key);
    setOpen(false);
  };

  const activeLabel =
    sections.flatMap((g) => g.items).find((i) => i.key === active)?.label || "Menu";

  return (
    <>
      <aside className="hidden w-56 shrink-0 lg:block">
        <nav className="sticky top-28 space-y-6">
          {sections.map((g) => (
            <div key={g.group}>
              <p className="mb-2 px-2 text-[10px] uppercase tracking-[0.2em] text-emerald-900/40">{g.group}</p>
              <div className="space-y-1">
                {g.items.map((item) => (
                  <button
                    key={item.key}
                    onClick={() => onSelect(item.key)}
                    className={`w-full rounded-lg px-3 py-2 text-left text-sm transition ${
                      active === item.key
                        ? "bg-emerald-900 font-medium text-emerald-50"
                        : "text-emerald-900/70 hover:bg-emerald-900/5"
                    }`}
                  >
                    {item.label}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </nav>
      </aside>

      {/* Mobile hamburger trigger */}
      <div className="lg:hidden">
        <button
          onClick={() => setOpen(true)}
          className="inline-flex w-full items-center justify-between rounded-xl bg-white px-4 py-3 text-sm font-medium text-emerald-950 shadow-sm ring-1 ring-emerald-900/10"
        >
          <span className="flex items-center gap-2">
            <Menu className="h-4 w-4 text-emerald-900/70" />
            {activeLabel}
          </span>
          <span className="text-[10px] uppercase tracking-widest text-emerald-900/40">Menu</span>
        </button>
      </div>

      {/* Mobile slide-out drawer */}
      {open && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div
            className="absolute inset-0 bg-emerald-950/40 backdrop-blur-sm"
            onClick={() => setOpen(false)}
          />
          <div className="absolute left-0 top-0 flex h-full w-[78%] max-w-xs flex-col bg-stone-50 shadow-2xl">
            <div className="flex items-center justify-between border-b border-emerald-900/10 px-5 py-4">
              <p className="font-display text-base text-emerald-950">Sections</p>
              <button
                onClick={() => setOpen(false)}
                className="rounded-lg p-1.5 text-emerald-900/60 hover:bg-emerald-900/5"
                aria-label="Close menu"
              >
                <X className="h-5 w-5" />
              </button>
            </div>
            <nav className="flex-1 overflow-y-auto px-3 py-4">
              {sections.map((g) => (
                <div key={g.group} className="mb-5">
                  <p className="mb-2 px-2 text-[10px] uppercase tracking-[0.2em] text-emerald-900/40">{g.group}</p>
                  <div className="space-y-1">
                    {g.items.map((item) => (
                      <button
                        key={item.key}
                        onClick={() => choose(item.key)}
                        className={`w-full rounded-lg px-3 py-2.5 text-left text-sm transition ${
                          active === item.key
                            ? "bg-emerald-900 font-medium text-emerald-50"
                            : "text-emerald-900/70 hover:bg-emerald-900/5"
                        }`}
                      >
                        {item.label}
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </nav>
          </div>
        </div>
      )}
    </>
  );
}