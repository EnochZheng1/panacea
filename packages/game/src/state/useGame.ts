import { loadContent, type ContentPack } from '@panacea/content';
import {
  analyzeFormula,
  applyAction,
  endTurn,
  newGame,
  observe,
  type Action,
  type ActionResult,
  type FormulaComponent,
  type GameState,
  type LedgerEntry,
} from '@panacea/sim';
import { useCallback, useMemo, useState } from 'react';

export const content: ContentPack = loadContent();

export interface Toast {
  id: number;
  text: string;
  tone: 'info' | 'error';
}

let toastId = 0;

/** The game session: real simulation state plus a few presentation-only bits. */
export function useGame(seed = 'panacea') {
  const [state, setState] = useState<GameState>(() => newGame(content, { seed }));
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [monthReport, setMonthReport] = useState<LedgerEntry | null>(null);
  const obs = useMemo(() => observe(state, content), [state]);

  const toast = useCallback((text: string, tone: Toast['tone'] = 'info') => {
    const id = ++toastId;
    setToasts((t) => [...t.slice(-3), { id, text, tone }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 4000);
  }, []);

  /** Applies a sequence of actions atomically: all succeed, or nothing changes. */
  const actAll = useCallback(
    (actions: Action[]): ActionResult => {
      let s = state;
      for (const a of actions) {
        const r = applyAction(s, a, content);
        if (!r.result.ok) {
          toast(r.result.error, 'error');
          return r.result;
        }
        s = r.state;
      }
      setState(s);
      return { ok: true };
    },
    [state, toast],
  );

  const act = useCallback((a: Action) => actAll([a]), [actAll]);

  const endMonth = useCallback(() => {
    if (state.meta.status !== 'running') return;
    const { state: next, report } = endTurn(state, content);
    setState(next);
    setMonthReport(report);
  }, [state]);

  /** What it would take to brew `bottles` of a formula: ingredients to order, and the cost. */
  const brewPlan = useCallback(
    (formulaId: string, bottles: number) => {
      const formula = state.lab.formulas.find((f) => f.id === formulaId);
      if (!formula) return null;
      const orders: { ingredientId: string; units: number; cents: number }[] = [];
      for (const c of formula.components) {
        const need = Math.ceil(
          c.amount * bottles - (state.lab.inventory[c.ingredientId] ?? 0) - 1e-9,
        );
        if (need > 0) {
          const unit = obs.ingredients.find((i) => i.id === c.ingredientId)?.costPerUnitCents ?? 0;
          orders.push({
            ingredientId: c.ingredientId,
            units: need,
            cents: Math.round(need * unit),
          });
        }
      }
      const bottling =
        bottles *
        (content.config.bottleCostCents +
          (content.byId.processes.get(formula.processId)?.extraCostPerBottleCents ?? 0));
      const total = orders.reduce((s, o) => s + o.cents, 0) + bottling;
      return { formula, orders, bottling, total };
    },
    [state, obs],
  );

  const newRun = useCallback((s: string) => {
    setState(newGame(content, { seed: s }));
    setMonthReport(null);
  }, []);

  return {
    state,
    obs,
    act,
    actAll,
    endMonth,
    brewPlan,
    toasts,
    toast,
    monthReport,
    setMonthReport,
    newRun,
  };
}

export type Game = ReturnType<typeof useGame>;

export const analyze = (components: FormulaComponent[], processId = 'simple_mix') =>
  analyzeFormula(components, processId, content);
