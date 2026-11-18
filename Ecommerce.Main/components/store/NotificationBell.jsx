import React, { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, Check } from "lucide-react";
import { ecommerceApi } from "@/api";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";

export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const queryClient = useQueryClient();
  const notificationsQuery = useQuery({
    queryKey: ["notifications"],
    queryFn: ecommerceApi.notifications.list,
    refetchInterval: 60_000,
  });
  const markRead = useMutation({
    mutationFn: ecommerceApi.notifications.markRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
  const markAllRead = useMutation({
    mutationFn: ecommerceApi.notifications.markAllRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
  const notifications = notificationsQuery.data?.items ?? [];
  const unreadCount = notificationsQuery.data?.unreadCount ?? 0;

  return (
    <>
      <button
        type="button"
        className="relative rounded-full p-2 text-emerald-900/70 transition hover:bg-emerald-900/5"
        aria-label={`Notifications${unreadCount ? `, ${unreadCount} unread` : ""}`}
        onClick={() => setOpen(true)}
      >
        <Bell className="h-4 w-4" />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 min-w-4 rounded-full bg-red-600 px-1 text-[10px] leading-4 text-white">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="font-display text-xl text-emerald-950">Notifications</DialogTitle>
          </DialogHeader>
          {notificationsQuery.isError && (
            <p className="text-sm text-destructive" role="alert">Could not load notifications.</p>
          )}
          {notifications.length > 0 && unreadCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              className="ml-auto"
              disabled={markAllRead.isPending}
              onClick={() => markAllRead.mutate()}
            >
              Mark all read
            </Button>
          )}
          <div className="max-h-[60vh] space-y-2 overflow-y-auto">
            {notificationsQuery.isPending ? (
              <p className="py-6 text-center text-sm text-emerald-900/50">Loading notifications…</p>
            ) : notifications.length === 0 ? (
              <p className="py-6 text-center text-sm text-emerald-900/50">You're all caught up.</p>
            ) : notifications.map(notification => (
              <article
                key={notification.notificationId}
                className={`rounded-xl p-4 ${notification.readOn ? "bg-stone-50" : "bg-emerald-50"}`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <h3 className="text-sm font-medium text-emerald-950">{notification.title}</h3>
                    <p className="mt-1 text-sm text-emerald-900/70">{notification.message}</p>
                    <time className="mt-2 block text-xs text-emerald-900/45" dateTime={notification.createdOn}>
                      {new Date(notification.createdOn).toLocaleString()}
                    </time>
                  </div>
                  {!notification.readOn && (
                    <button
                      type="button"
                      className="shrink-0 rounded-full p-2 text-emerald-800 hover:bg-emerald-900/10"
                      aria-label="Mark notification read"
                      disabled={markRead.isPending}
                      onClick={() => markRead.mutate(notification.notificationId)}
                    >
                      <Check className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </article>
            ))}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
