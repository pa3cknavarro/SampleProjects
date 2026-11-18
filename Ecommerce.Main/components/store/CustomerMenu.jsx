import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { base44 } from "@/api/base44Client";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { ChevronDown, User, Package, KeyRound, Loader2, ChevronRight, Check } from "lucide-react";

const STATUS_COLOR = {
  Pending: "bg-amber-100 text-amber-700",
  Processing: "bg-blue-100 text-blue-700",
  Shipped: "bg-indigo-100 text-indigo-700",
  Delivered: "bg-stone-200 text-ink",
  Cancelled: "bg-red-100 text-red-700",
};

export default function CustomerMenu({ user, customer, orders, onCustomerUpdate }) {
  const [open, setOpen] = useState(null);
  const [pw, setPw] = useState("");
  const [pw2, setPw2] = useState("");
  const [pwErr, setPwErr] = useState("");
  const [pwLoading, setPwLoading] = useState(false);
  const [pwDone, setPwDone] = useState(false);
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [acctSaving, setAcctSaving] = useState(false);
  const [acctDone, setAcctDone] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    setEmail(customer?.email || "");
    setPhone(customer?.phone || "");
  }, [customer]);

  const changePassword = async () => {
    setPwErr("");
    if (!pw || pw.length < 4) {
      setPwErr("Password must be at least 4 characters");
      return;
    }
    if (pw !== pw2) {
      setPwErr("Passwords do not match");
      return;
    }
    setPwLoading(true);
    try {
      await base44.entities.AppUser.update(user.id, { password: pw });
      setPw("");
      setPw2("");
      setPwDone(true);
      setTimeout(() => {
        setPwDone(false);
        setOpen(null);
      }, 1200);
    } catch (e) {
      setPwErr(e.message || "Could not change password");
    }
    setPwLoading(false);
  };

  const saveAccount = async () => {
    if (!customer) return;
    setAcctSaving(true);
    try {
      const updated = await base44.entities.Customer.update(customer.id, { email, phone });
      if (onCustomerUpdate) onCustomerUpdate(updated);
      setAcctDone(true);
      setTimeout(() => {
        setAcctDone(false);
        setOpen(null);
      }, 1200);
    } catch (e) {
      setAcctSaving(false);
    }
  };

  const openOrder = (o) => {
    setOpen(null);
    navigate(`/orders/${o.id}`);
  };

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button className="flex items-center gap-1 rounded-full px-1 py-1 text-sm text-emerald-900/70 transition hover:text-emerald-950">
            <User className="h-4 w-4 sm:hidden" />
            <span className="hidden max-w-[150px] truncate sm:block">
              {user?.company_name || user?.user_name || "Customer"}
            </span>
            <ChevronDown className="hidden h-3.5 w-3.5 sm:block" />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
          <DropdownMenuItem onClick={() => setOpen("account")}>
            <User className="mr-2 h-4 w-4" /> Account Details
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => setOpen("orders")}>
            <Package className="mr-2 h-4 w-4" /> Order History
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => setOpen("password")}>
            <KeyRound className="mr-2 h-4 w-4" /> Change Password
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Account details */}
      <Dialog open={open === "account"} onOpenChange={(o) => !o && setOpen(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="font-display text-xl text-emerald-950">Account Details</DialogTitle>
          </DialogHeader>
          {customer ? (
            <>
              <dl className="grid grid-cols-3 gap-y-3 text-sm">
                <dt className="text-emerald-900/50">Company</dt>
                <dd className="col-span-2 text-emerald-950">{customer.name}</dd>
                <dt className="text-emerald-900/50">Class</dt>
                <dd className="col-span-2 text-emerald-950">{customer.class || "—"}</dd>
                <dt className="text-emerald-900/50">Contact</dt>
                <dd className="col-span-2 text-emerald-950">{customer.contact_person || "—"}</dd>
                <dt className="text-emerald-900/50">Email</dt>
                <dd className="col-span-2">
                  <Input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="h-8 text-sm"
                  />
                </dd>
                <dt className="text-emerald-900/50">Phone</dt>
                <dd className="col-span-2">
                  <Input
                    type="text"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="h-8 text-sm"
                  />
                </dd>
                <dt className="text-emerald-900/50">Province</dt>
                <dd className="col-span-2 text-emerald-950">{customer.province || "—"}</dd>
                <dt className="text-emerald-900/50">City</dt>
                <dd className="col-span-2 text-emerald-950">{customer.city || "—"}</dd>
                <dt className="text-emerald-900/50">Barangay</dt>
                <dd className="col-span-2 text-emerald-950">{customer.barangay || "—"}</dd>
                <dt className="text-emerald-900/50">Address</dt>
                <dd className="col-span-2 text-emerald-950">{customer.address || "—"}</dd>
              </dl>
              <div className="flex items-center justify-end gap-3 pt-4">
                {acctDone && (
                  <span className="inline-flex items-center gap-1 text-sm text-ink">
                    <Check className="h-4 w-4" /> Saved
                  </span>
                )}
                <Button onClick={saveAccount} disabled={acctSaving}>
                  {acctSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Save Changes
                </Button>
              </div>
            </>
          ) : (
            <p className="py-6 text-center text-sm text-emerald-900/50">
              No customer record linked to this account.
            </p>
          )}
        </DialogContent>
      </Dialog>

      {/* Order details — summary list */}
      <Dialog open={open === "orders"} onOpenChange={(o) => !o && setOpen(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="font-display text-xl text-emerald-950">Order History</DialogTitle>
          </DialogHeader>
          {orders && orders.length ? (
            <div className="max-h-[60vh] space-y-2 overflow-y-auto">
              {orders.map((o) => (
                <button
                  key={o.id}
                  onClick={() => openOrder(o)}
                  className="flex w-full flex-wrap items-center justify-between gap-x-3 gap-y-2 rounded-xl bg-stone-50 p-4 text-left ring-1 ring-emerald-900/10 transition hover:bg-stone-100"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-emerald-950">{o.order_number}</p>
                    <p className="text-xs text-emerald-900/50">{o.order_date || "—"}</p>
                  </div>
                  <div className="flex shrink-0 items-center gap-2 sm:gap-3">
                    <span
                      className={`rounded-full px-2.5 py-1 text-[11px] font-medium ${
                        STATUS_COLOR[o.status] || "bg-stone-100 text-stone-700"
                      }`}
                    >
                      {o.status || "Pending"}
                    </span>
                    <span className="text-sm font-medium text-emerald-950">
                      ₱{(o.total || 0).toLocaleString()}
                    </span>
                    <ChevronRight className="h-4 w-4 text-emerald-900/40" />
                  </div>
                </button>
              ))}
            </div>
          ) : (
            <p className="py-6 text-center text-sm text-emerald-900/50">No orders yet.</p>
          )}
        </DialogContent>
      </Dialog>

      {/* Change password */}
      <Dialog open={open === "password"} onOpenChange={(o) => !o && setOpen(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle className="font-display text-xl text-emerald-950">Change Password</DialogTitle>
          </DialogHeader>
          {pwDone ? (
            <p className="py-6 text-center text-sm text-ink">Password updated successfully.</p>
          ) : (
            <div className="space-y-4">
              {pwErr && (
                <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{pwErr}</div>
              )}
              <div className="space-y-2">
                <Label htmlFor="npw">New Password</Label>
                <Input
                  id="npw"
                  type="password"
                  value={pw}
                  onChange={(e) => setPw(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="npw2">Confirm Password</Label>
                <Input
                  id="npw2"
                  type="password"
                  value={pw2}
                  onChange={(e) => setPw2(e.target.value)}
                />
              </div>
              <Button className="w-full" onClick={changePassword} disabled={pwLoading}>
                {pwLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Update Password
              </Button>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}