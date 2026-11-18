import React, { useState, useEffect } from "react";
import { Bell } from "lucide-react";
import { base44 } from "@/api/base44Client";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
} from "@/components/ui/dropdown-menu";

export default function NotificationBell() {
  const [items, setItems] = useState([]);
  const [open, setOpen] = useState(false);

  const load = async () => {
    try {
      const all = await base44.entities.Notification.list("-created_date", 50);
      setItems(all);
    } catch (e) {}
  };

  useEffect(() => {
    load();
  }, []);

  const unread = items.filter((n) => !n.read).length;

  const markAllRead = async () => {
    const pending = items.filter((n) => !n.read);
    if (!pending.length) return;
    await Promise.all(
      pending.map((n) => base44.entities.Notification.update(n.id, { read: true }))
    );
    load();
  };

  const clearAll = async () => {
    try {
      await base44.entities.Notification.deleteMany({});
      setItems([]);
    } catch (e) {}
  };

  return (
    <DropdownMenu
      open={open}
      onOpenChange={(o) => {
        setOpen(o);
        if (o && unread > 0) markAllRead();
      }}
    >
      <DropdownMenuTrigger asChild>
        <button className="relative rounded-full bg-emerald-900/5 p-2 text-emerald-900 transition hover:bg-emerald-900/10 sm:p-2.5">
          <Bell className="h-4 w-4" />
          {unread > 0 && (
            <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-semibold text-white">
              {unread}
            </span>
          )}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-80 max-w-[calc(100vw-1.5rem)] p-0">
        <div className="flex items-center justify-between border-b border-emerald-900/10 px-4 py-3">
          <p className="font-display text-sm text-emerald-950">Notifications</p>
          {items.length > 0 && (
            <div className="flex items-center gap-1">
              <button
                onClick={markAllRead}
                className="rounded-full px-2.5 py-1 text-[11px] font-medium text-emerald-900/70 transition hover:bg-emerald-900/5"
              >
                Read all
              </button>
              <button
                onClick={clearAll}
                className="rounded-full px-2.5 py-1 text-[11px] font-medium text-red-600 transition hover:bg-red-50"
              >
                Clear all
              </button>
            </div>
          )}
        </div>
        <div className="max-h-80 overflow-y-auto">
          {items.length === 0 ? (
            <p className="px-4 py-6 text-center text-xs text-emerald-900/50">No notifications yet.</p>
          ) : (
            items.map((n) => (
              <div key={n.id} className="border-b border-emerald-900/5 px-4 py-3 last:border-0">
                <div className="flex items-start gap-2.5">
                  <span
                    className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${
                      n.read ? "bg-emerald-900/20" : "bg-amber-500"
                    }`}
                  />
                  <div>
                    <p className="text-sm font-medium text-emerald-950">{n.title}</p>
                    <p className="text-xs leading-relaxed text-emerald-900/60">{n.message}</p>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}