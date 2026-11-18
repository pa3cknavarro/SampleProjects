import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import AdminSidebar from "@/components/admin/AdminSidebar";
import EntityManager from "@/components/admin/EntityManager";
import Dashboard from "@/components/admin/Dashboard";
import Reports from "@/components/admin/Reports";
import { SECTIONS } from "@/components/admin/sections";
import { getStorefrontUser } from "@/lib/storefrontAuth";
import { ArrowLeft } from "lucide-react";
import BrandLogo from "@/components/BrandLogo";

export default function Admin() {
  const [activeKey, setActiveKey] = useState("dashboard");
  const [ready, setReady] = useState(false);
  const [role, setRole] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    const su = getStorefrontUser();
    if (!su || su.role === "User") {
      navigate("/");
      return;
    }
    setRole(su.role);
    setReady(true);
  }, [navigate]);

  const findSection = (key) => {
    for (const g of SECTIONS) {
      const item = g.items.find((i) => i.key === key);
      if (item) return item;
    }
    return SECTIONS[0].items[0];
  };

  const active = findSection(activeKey);

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-stone-50">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-900/10 border-t-emerald-900" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-stone-50">
      <header className="sticky top-0 z-30 border-b border-emerald-900/10 bg-stone-50/80 backdrop-blur-xl">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4 lg:px-10">
          <div className="flex items-center gap-3">
            <BrandLogo size="sm" />
            <span className="h-7 w-px bg-emerald-900/15" />
            <p className="font-display text-lg text-emerald-950">Admin Console</p>
          </div>
          <button
            onClick={() => navigate("/store")}
            className="inline-flex items-center gap-1.5 rounded-full px-3 py-2 text-xs font-medium text-emerald-900/70 ring-1 ring-emerald-900/15 transition hover:bg-emerald-900/5"
          >
            <ArrowLeft className="h-4 w-4" /> Store
          </button>
        </div>
      </header>

      <div className="mx-auto flex max-w-7xl flex-col gap-8 px-5 py-8 lg:flex-row lg:px-10">
        <AdminSidebar sections={SECTIONS} active={activeKey} onSelect={setActiveKey} />
        <main className="min-w-0 flex-1">
          {active.component === "dashboard" ? (
            <Dashboard />
          ) : active.component === "reports" ? (
            <Reports />
          ) : (
            <EntityManager section={active} userRole={role} />
          )}
        </main>
      </div>
    </div>
  );
}