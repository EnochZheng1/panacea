export * from './core/index.ts';
export {
  applyAction,
  applyActionInPlace,
  canonicalAction,
  endTurn,
  endTurnInPlace,
  newGame,
  replay,
  PHASES,
  type NewGameOptions,
} from './game.ts';
export { checkInvariants } from './invariants.ts';
export { deserializeGame, serializeGame, type SaveFile } from './save.ts';
export {
  legalActions,
  looksFor,
  moodFor,
  observe,
  type ActionSpec,
  type Looks,
  type Mood,
  type Observation,
  type ObservedCustomer,
  type ObservedInvestment,
  type ObservedDeath,
} from './api/machine.ts';
export {
  analyzeFormula,
  claimAppeal,
  claimBoldness,
  validateClaims,
  validateFormula,
  type FormulaAnalysis,
} from './systems/formula.ts';
export {
  formulaUnitCost,
  maxBrewable,
  stillBottles,
  stockOf,
  stockValue,
  totalStock,
  type StockValue,
} from './systems/lab.ts';
export { acuteDanger, feltEffect, overdoseChance, reliefScore } from './systems/pharmacology.ts';
export {
  investmentProgress,
  investRefusal,
  isHeld,
  orderPriceFactor,
  wholesaleOrder,
  wholesalePrice,
} from './systems/investments.ts';
export {
  availableChoices,
  capacitySteps,
  nextUnlocks,
  type CapacityStep,
  type UnlockProgress,
} from './systems/progression.ts';
export {
  bribeCostCents,
  bribeExposureChance,
  implicationChance,
  INQUEST_EVENT_ID,
} from './systems/scrutiny.ts';
export {
  blamedOnTheBottle,
  reportedCauseOf,
  wardRemembers,
  type WardRemembers,
} from './systems/ward.ts';
export { bottlesWanted, isRuined, wordOfStrength } from './systems/market.ts';
export {
  evaluateRequirement,
  requirementProgress,
  type ConditionProgress,
  type RequirementStatus,
} from './systems/requirements.ts';
