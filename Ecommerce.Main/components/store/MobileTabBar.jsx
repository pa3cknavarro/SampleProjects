import React from "react";
import { Home, ShoppingBag, ShieldCheck, LogOut } from "lucide-react";

export default function MobileTabBar({ onCart, onAdmin, onLogout, cartCount = 0, isAdmin = false }) {
  const Tab = ({ icon: Icon, label, onClick, badge }) => (
    <button onClick={onClick} className="flex flex-1 flex-col items-center gap-1 py-2.5 text-emerald-900/70">
      <span className="relative">
        <Icon className="h-5 w-5" />
        {badge > 0 && (
          <span className="absolute -right-2 -top-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[9px] font-bold text-primary-foreground">
            {badge}
          </span>
        )}
      </span>
      <span className="text-[10px] font-medium tracking-wide">{label}</span>
    </button>
  );

  return (
    <nav className="fixed inset-x-0 bottom-0 z-40 flex border-t border-emerald-900/10 bg-stone-50/95 backdrop-blur-xl lg:hidden">
      <Tab icon={Home} label="Home" onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })} />
      <Tab icon={ShoppingBag} label="Cart" onClick={onCart} badge={cartCount} />
      {isAdmin && <Tab icon={ShieldCheck} label="Admin" onClick={onAdmin} />}
      <Tab icon={LogOut} label="Logout" onClick={onLogout} />
    </nav>
  );
}