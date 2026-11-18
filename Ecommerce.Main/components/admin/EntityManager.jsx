import React, { useState, useEffect } from "react";
import { ecommerceApi } from "@/api";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";
import { Plus, Pencil, Trash2, Loader2 } from "lucide-react";

export default function EntityManager({ section, userRole }) {
  const { entity, title, columns, fields } = section;
  const [rows, setRows] = useState(/** @type {Record<string, unknown>[]} */ ([]));
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState(/** @type {Record<string, unknown> | null} */ (null));
  const [form, setForm] = useState(/** @type {Record<string, unknown>} */ ({}));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [optionsCache, setOptionsCache] = useState(/** @type {Record<string, Record<string, unknown>[]>} */ ({}));

  const load = async () => {
    setLoading(true);
    try {
      const response = await ecommerceApi.admin.list(entity, 200);
      setRows(response.items);
      setError("");
    } catch (e) {
      setRows([]);
      setError(e.message || "Could not load records.");
    } finally {
      setLoading(false);
    }
  };

  const loadOptions = async () => {
    const opts = /** @type {Record<string, Record<string, unknown>[]>} */ ({});
    for (const f of fields) {
      if (f.optionsEntity) {
        try {
          const response = await ecommerceApi.admin.list(f.optionsEntity.entity, 500);
          opts[f.key] = response.items;
        } catch (e) {
          opts[f.key] = [];
          setError(e.message || `Could not load ${f.label} options.`);
        }
      }
    }
    setOptionsCache(opts);
  };

  useEffect(() => {
    load();
    loadOptions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entity]);

  /** @param {Record<string, unknown> | null} row @returns {Record<string, unknown>} */
  const buildInitial = (row) => {
    const init = /** @type {Record<string, unknown>} */ ({});
    fields.forEach((f) => {
      if (f.type === "checkbox") init[f.key] = row ? !!row[f.key] : false;
      else if (entity === "Order" && f.key === "orderStatus" && row) {
        const status = row.orderStatus;
        init[f.key] = typeof status === "number"
          ? ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"][status] || "Pending"
          : "Pending";
      }
      else init[f.key] = row ? row[f.key] ?? "" : "";
    });
    return init;
  };

  const openAdd = () => {
    setEditing(null);
    setForm(buildInitial(null));
    setError("");
    setDialogOpen(true);
  };

  const openEdit = (row) => {
    setEditing(row);
    setForm(buildInitial(row));
    setError("");
    setDialogOpen(true);
  };

  const setField = (key, val) =>
    setForm((prev) => {
      const next = { ...prev, [key]: val };
      fields.forEach((g) => {
        if (g.dependsOn && g.dependsOn.includes(key)) next[g.key] = "";
      });
      return next;
    });

  const getOptions = (f) => {
    const recs = optionsCache[f.key] || [];
    const filtered = f.dependsOn
      ? recs.filter((r) => f.dependsOn.every((k) => !form[k] || r[k] === form[k]))
      : recs;
    return filtered.map((row) => {
      const value = row[f.optionsEntity.valueKey || f.optionsEntity.labelKey];
      const label = row[f.optionsEntity.labelKey];
      return {
        value: typeof value === "string" || typeof value === "number" ? value : "",
        label: String(label ?? ""),
      };
    });
  };

  const fieldValue = (value) =>
    typeof value === "string" || typeof value === "number" ? value : "";

  const save = async () => {
    setError("");
    for (const f of fields) {
      if (f.required && f.type !== "checkbox" && !form[f.key]) {
        setError(`${f.label} is required`);
        return;
      }
    }
    const pw = fields.find((f) => f.key === "password");
    const cpw = fields.find((f) => f.key === "confirm_password");
    if (pw && cpw && form[pw.key] !== form[cpw.key]) {
      setError("Passwords do not match");
      return;
    }
    if (entity === "AppUser" && !editing && !form.password) {
      setError("Password is required for a new user.");
      return;
    }
    const payload = /** @type {Record<string, unknown>} */ ({});
    fields.forEach((f) => {
      if (f.submit === false) return;
      if (f.type === "checkbox") payload[f.key] = !!form[f.key];
      else if (form[f.key] !== "" && form[f.key] !== null && form[f.key] !== undefined) payload[f.key] = form[f.key];
    });
    setSaving(true);
    try {
      if (editing) {
        await ecommerceApi.admin.update(entity, editing, payload);
      } else {
        await ecommerceApi.admin.create(entity, payload);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      setError(err.message || "Could not save");
    } finally {
      setSaving(false);
    }
  };

  const remove = async (row) => {
    if (!window.confirm("Delete this record?")) return;
    try {
      await ecommerceApi.admin.delete(entity, row);
      await load();
    } catch (e) {
      setError(e.message || "Could not delete");
    }
  };

  const rowKey = (row) => {
    if (entity === "City") return `${row.provinceId}:${row.cityId}`;
    if (entity === "Barangay") return `${row.provinceId}:${row.cityId}:${row.barangayId}`;
    return row[{
      Customer: "customerId", CustomerClass: "customerClassId", Product: "productId",
      Category: "categoryId", UnitOfMeasure: "id", Order: "orderId",
      Province: "provinceId", AppUser: "id", UnitPrice: "unitPriceId",
      AreaAvailable: "areaAvailableId",
    }[entity]] || JSON.stringify(row);
  };

  const q = search.trim().toLowerCase();
  const filtered = q
    ? rows.filter((r) => columns.some((c) => String(r[c.key] ?? "").toLowerCase().includes(q)))
    : rows;
  const canDelete = userRole === "Admin" && !section.readOnly;

  const selectCls =
    "flex h-11 w-full rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus:outline-none focus:ring-2 focus:ring-ring";

  const inputType = (f) =>
    ["password", "email", "number", "date"].includes(f.type) ? f.type : "text";

  return (
    <div>
      <div className="mb-5">
        <div className="mb-4">
          <h2 className="font-display text-2xl text-emerald-950">{title}</h2>
          <p className="text-xs text-emerald-900/50">{filtered.length} record(s)</p>
        </div>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <Input
            placeholder="Search..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="sm:max-w-sm"
          />
          {!section.readOnly && section.allowCreate !== false && <button
            onClick={openAdd}
            className="sm:ml-auto inline-flex items-center gap-1.5 rounded-full bg-emerald-900 px-4 py-2.5 text-xs font-medium text-emerald-50 transition hover:bg-emerald-800"
          >
            <Plus className="h-4 w-4" /> Add
          </button>}
        </div>
      </div>

      {error && <div className="mb-4 rounded-lg bg-destructive/10 p-3 text-sm text-destructive" role="alert">{error}</div>}
      {loading ? (
        <p className="py-10 text-center text-sm text-emerald-900/40">Loading…</p>
      ) : rows.length === 0 ? (
        <p className="py-10 text-center text-sm text-emerald-900/40">No records yet.</p>
      ) : (
        <div className="overflow-x-auto rounded-2xl bg-white ring-1 ring-emerald-900/10">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-emerald-900/10 bg-stone-50 text-left text-[11px] uppercase tracking-widest text-emerald-900/50">
                {columns.map((c) => (
                  <th key={c.key} className="px-4 py-3 font-medium">{c.label}</th>
                ))}
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((row) => (
                <tr key={rowKey(row)} className="border-b border-emerald-900/5 last:border-0">
                  {columns.map((c) => (
                    <td key={c.key} className="px-4 py-3 text-emerald-950">
                      {c.render ? c.render(row[c.key]) : (String(row[c.key] ?? "—"))}
                    </td>
                  ))}
                  <td className="px-4 py-3">
                    {!section.readOnly && (section.allowUpdate !== false || canDelete) && <div className="flex justify-end gap-2">
                      {section.allowUpdate !== false && <button
                        onClick={() => openEdit(row)}
                        className="rounded-full p-2 text-emerald-900/60 ring-1 ring-emerald-900/15 transition hover:bg-emerald-900/5"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </button>}
                      {canDelete && section.allowDelete !== false && (
                        <button
                          onClick={() => remove(row)}
                          className="rounded-full p-2 text-red-600 ring-1 ring-red-200 transition hover:bg-red-50"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      )}
                    </div>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="font-display text-xl text-emerald-950">
              {editing ? "Edit" : "Add"} {title}
            </DialogTitle>
          </DialogHeader>
          {error && (
            <div className="mb-3 rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
          )}
          <div className="grid max-h-[60vh] gap-4 overflow-y-auto py-2">
            {fields.map((f) => (
              <div key={f.key} className="space-y-2">
                {f.type === "checkbox" ? (
                  <label className="flex items-center gap-2.5 text-sm text-emerald-950">
                    <Checkbox checked={!!form[f.key]} onCheckedChange={(v) => setField(f.key, v)} />
                    {f.label}
                  </label>
                ) : (
                  <>
                    <Label>
                      {f.label}
                      {f.required && <span className="text-red-500"> *</span>}
                    </Label>
                    {f.type === "textarea" ? (
                      <Textarea
                        value={fieldValue(form[f.key])}
                        onChange={(e) => setField(f.key, e.target.value)}
                        rows={2}
                      />
                    ) : f.type === "select" ? (
                      <select
                        value={fieldValue(form[f.key])}
                        onChange={(e) => setField(f.key, e.target.value)}
                        className={selectCls}
                        disabled={editing && (f.readOnlyOnEdit || (entity === "AppUser" && ["companyId", "username"].includes(f.key)))}
                      >
                        <option value="">— Select —</option>
                        {(f.options
                          ? f.options.map((option) => ({ value: option, label: option }))
                          : getOptions(f)
                        ).map((option, i) => (
                          <option key={`${option.value}-${i}`} value={option.value}>{option.label}</option>
                        ))}
                      </select>
                    ) : (
                      <Input
                        type={inputType(f)}
                        value={fieldValue(form[f.key])}
                        disabled={editing && (f.readOnlyOnEdit || (entity === "AppUser" && ["companyId", "username"].includes(f.key)))}
                        onChange={(e) =>
                          setField(f.key, f.type === "number" ? Number(e.target.value) : e.target.value)
                        }
                      />
                    )}
                  </>
                )}
              </div>
            ))}
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
              Cancel
            </Button>
            <Button onClick={save} disabled={saving}>
              {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {editing ? "Save" : "Create"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}