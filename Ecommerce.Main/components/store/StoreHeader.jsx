import React from "react";
import { ShoppingBag, LogOut, ShieldCheck } from "lucide-react";
import CustomerMenu from "@/components/store/CustomerMenu";
import NotificationBell from "@/components/store/NotificationBell";
import BrandLogo from "@/components/BrandLogo";

export default function StoreHeader({ user, customer, orders, count, onCartClick, onLogin, onLogout, onAdmin, onCustomerUpdate }) {
  return (
    <header className="sticky top-0 z-30 border-b border-emerald-900/10 bg-stone-50/80 backdrop-blur-xl">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-3 lg:px-10">
        <BrandLogo size="md" className="h-9 sm:h-11" />

        <div className="flex items-center gap-1.5 sm:gap-3">
          {user ? (
            <>
              <CustomerMenu user={user} customer={customer} orders={orders} onCustomerUpdate={onCustomerUpdate} />
              {user?.role && user.role !== "User" && (
                <button
                  onClick={onAdmin}
                  className="relative inline-flex items-center gap-1.5 rounded-full bg-amber-500 px-2.5 py-2 text-xs font-semibold text-white transition hover:bg-amber-600 sm:px-3 sm:py-2.5"
                >
                  <ShieldCheck className="h-4 w-4" />
                  <span className="hidden sm:inline">Admin</span>
                </button>
              )}
              <NotificationBell />
              <button onClick={onCartClick} className="relative rounded-full bg-primary p-2 text-primary-foreground transition hover:bg-brand-dark sm:p-2.5">
                <ShoppingBag className="h-4 w-4" />
                {count > 0 && (
                  <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-white px-1 text-[10px] font-semibold text-primary ring-1 ring-emerald-900/10">
                    {count}
                  </span>
                )}
              </button>
              <button onClick={onLogout} className="rounded-full p-2 text-emerald-900/50 transition hover:bg-emerald-900/5 sm:p-2.5">
                <LogOut className="h-4 w-4" />
              </button>
            </>
          ) : (
            <button
              onClick={onLogin}
              className="rounded-full bg-primary px-5 py-2.5 text-xs font-medium tracking-wide text-primary-foreground transition hover:bg-brand-dark"
            >
              Sign In
            </button>
          )}
        </div>
      </div>
    </header>
  );
}