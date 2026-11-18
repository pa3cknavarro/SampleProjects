import React from "react";
import { Wheat, Sprout, FlaskConical, Shovel, Bone, Bath, LayoutGrid } from "lucide-react";

export const CATEGORIES = [
  { name: "All Products", icon: LayoutGrid },
  { name: "Animal Feeds", icon: Wheat },
  { name: "Seeds", icon: Sprout },
  { name: "Fertilizers", icon: FlaskConical },
  { name: "Farm Tools", icon: Shovel },
  { name: "Pet Food", icon: Bone },
  { name: "Pet Care", icon: Bath },
];

export default function CategoryList({ active, onSelect, vertical }) {
  return (
    <nav className={`${vertical ? "flex-col" : "flex lg:flex-col"} gap-2 ${vertical ? "" : "overflow-x-auto lg:overflow-visible pb-2 lg:pb-0"}`}>
      {CATEGORIES.map(({ name, icon: Icon }) => {
        const isActive = active === name;
        return (
          <button
            key={name}
            onClick={() => onSelect(name)}
            className={`group flex items-center gap-3 whitespace-nowrap rounded-xl px-4 py-3 text-sm transition-all duration-300 ${
              isActive
                ? "bg-emerald-900 text-emerald-50 shadow-lg shadow-emerald-900/20"
                : "text-emerald-900/70 hover:bg-emerald-900/5"
            }`}
          >
            <Icon className={`w-4 h-4 transition-transform duration-300 ${isActive ? "" : "group-hover:scale-110"}`} />
            <span className="tracking-wide">{name}</span>
          </button>
        );
      })}
    </nav>
  );
}