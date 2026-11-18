import React, { useState, useEffect } from "react";
import { base44 } from "@/api/base44Client";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";
import { Plus, Pencil, Trash2, Loader2 } from "lucide-react";

export default function EntityManager({ section, userRole }) {
  const { entity, title, columns, fields } = section;
  const [rows, setRows] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [optionsCache, setOptionsCache] = useState({});

  const load = async () => {
    setLoading(true);
    try {
      const all = await base44.entities[entity].list("-created_date", 200);
      setRows(all);
    } catch (e) {
      setRows([]);
    }
    setLoading(false);
  };

  const loadOptions = async () => {
    const opts = {};
    for (const f of fields) {
      if (f.optionsEntity) {
        try {
          const recs = await base44.entities[f.optionsEntity.entity].list("-created_date", 500);
          opts[f.key] = recs;
        } catch (e) {
          opts[f.key] = [];
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

  const buildInitial = (row) => {
    const init = {};
    fields.forEach((f) => {
      if (f.type === "checkbox") init[f.key] = row ? !!row[f.key] : false;
      else if (f.type === "priceList") init[f.key] = row && row[f.key] ? row[f.key] : [];
      else if (f.type === "multiselect") init[f.key] = row && row[f.key] ? row[f.key] : [];
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
    return filtered.map((r) => r[f.optionsEntity.labelKey]);
  };

  const updatePriceRow = (key, idx, field, val) =>
    setForm((prev) => {
      const arr = [...(prev[key] || [])];
      arr[idx] = { ...arr[idx], [field]: val };
      return { ...prev, [key]: arr };
    });
  const addPriceRow = (key) =>
    setForm((prev) => ({ ...prev, [key]: [...(prev[key] || []), { unit: "", price: 0 }] }));
  const removePriceRow = (key, idx) =>
    setForm((prev) => ({ ...prev, [key]: (prev[key] || []).filter((_, i) => i !== idx) }));
  const toggleMulti = (key, opt, checked) =>
    setForm((prev) => {
      const arr = prev[key] || [];
      return { ...prev, [key]: checked ? [...arr, opt] : arr.filter((x) => x !== opt) };
    });

  const save = async () => {
    setError("");
    for (const f of fields) {
      if (f.required && f.type !== "checkbox" && f.type !== "priceList" && f.type !== "multiselect" && !form[f.key]) {
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
    const payload = {};
    fields.forEach((f) => {
      if (f.submit === false) return;
      if (f.type === "checkbox") payload[f.key] = !!form[f.key];
      else if (f.type === "priceList") payload[f.key] = (form[f.key] || []).filter((r) => r.unit);
      else if (f.type === "multiselect") payload[f.key] = form[f.key] || [];
      else if (form[f.key] !== "" && form[f.key] !== null && form[f.key] !== undefined) payload[f.key] = form[f.key];
    });
    setSaving(true);
    try {
      if (editing) {
        await base44.entities[entity].update(editing.id, payload);
      } else {
        await base44.entities[entity].create(payload);
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
      await base44.entities[entity].delete(row.id);
      setRows((r) => r.filter((x) => x.id !== row.id));
    } catch (e) {
      alert(e.message || "Could not delete");
    }
  };

  const q = search.trim().toLowerCase();
  const filtered = q
    ? rows.filter((r) => columns.some((c) => String(r[c.key] ?? "").toLowerCase().includes(q)))
    : rows;
  const canDelete = userRole !== "Agent";

  const selectCls =
    "flex h-11 w-full rounded-md border border-input bg-background px-3 text-sm ring-offset-background focus:outline-none focus:ring-2 focus:ring-ring";

  const inputType = (f) =>
    f.type === "password" ? "password" : f.type === "number" ? "number" : f.type === "date" ? "date" : "text";

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
          <button
            onClick={openAdd}
            className="sm:ml-auto inline-flex items-center gap-1.5 rounded-full bg-emerald-900 px-4 py-2.5 text-xs font-medium text-emerald-50 transition hover:bg-emerald-800"
          >
            <Plus className="h-4 w-4" /> Add
          </button>
        </div>
      </div>

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
                <tr key={row.id} className="border-b border-emerald-900/5 last:border-0">
                  {columns.map((c) => (
                    <td key={c.key} className="px-4 py-3 text-emerald-950">
                      {c.render ? c.render(row[c.key]) : (row[c.key] ?? "—")}
                    </td>
                  ))}
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <button
                        onClick={() => openEdit(row)}
                        className="rounded-full p-2 text-emerald-900/60 ring-1 ring-emerald-900/15 transition hover:bg-emerald-900/5"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                      {canDelete && (
                        <button
                          onClick={() => remove(row)}
                          className="rounded-full p-2 text-red-600 ring-1 ring-red-200 transition hover:bg-red-50"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      )}
                    </div>
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
                        value={form[f.key] || ""}
                        onChange={(e) => setField(f.key, e.target.value)}
                        rows={2}
                      />
                    ) : f.type === "select" ? (
                      <select
                        value={form[f.key] || ""}
                        onChange={(e) => setField(f.key, e.target.value)}
                        className={selectCls}
                      >
                        <option value="">— Select —</option>
                        {(f.options || getOptions(f)).map((opt, i) => (
                          <option key={`${opt}-${i}`} value={opt}>{opt}</option>
                        ))}
                      </select>
                    ) : f.type === "priceList" ? (
                      <div className="space-y-2">
                        {(form[f.key] || []).map((row, idx) => (
                          <div key={idx} className="grid grid-cols-[1fr_auto_auto] items-center gap-2">
                            <select
                              value={row.unit || ""}
                              onChange={(e) => updatePriceRow(f.key, idx, "unit", e.target.value)}
                              className={selectCls}
                            >
                              <option value="">Unit</option>
                              {(optionsCache[f.key] || []).map((r) => (
                                <option key={r.id || r.name} value={r.name}>{r.name}</option>
                              ))}
                            </select>
                            <Input
                              type="number"
                              value={row.price || 0}
                              onChange={(e) => updatePriceRow(f.key, idx, "price", Number(e.target.value))}
                              className="w-28"
                            />
                            <button
                              type="button"
                              onClick={() => removePriceRow(f.key, idx)}
                              className="rounded-full p-2 text-red-600 ring-1 ring-red-200 hover:bg-red-50"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          </div>
                        ))}
                        <button
                          type="button"
                          onClick={() => addPriceRow(f.key)}
                          className="inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-medium text-emerald-900 ring-1 ring-emerald-900/15 hover:bg-emerald-900/5"
                        >
                          <Plus className="h-3.5 w-3.5" /> Add unit
                        </button>
                      </div>
                    ) : f.type === "multiselect" ? (
                      <div className="grid grid-cols-2 gap-2 rounded-lg bg-stone-50 p-3 sm:grid-cols-3">
                        {f.options.map((opt) => (
                          <label key={opt} className="flex items-center gap-2 text-sm text-emerald-950">
                            <Checkbox
                              checked={(form[f.key] || []).includes(opt)}
                              onCheckedChange={(v) => toggleMulti(f.key, opt, v)}
                            />
                            {opt}
                          </label>
                        ))}
                      </div>
                    ) : (
                      <Input
                        type={inputType(f)}
                        value={form[f.key] || ""}
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