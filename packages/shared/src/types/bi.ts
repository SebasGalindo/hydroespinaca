// ==================== Cost Configuration Types ====================

export interface CostConfigVersion {
  id: string;
  currency: string;
  electricityCostPerKwh: number;
  waterCostPerLiter: number;
  nutrientCostPerLiter: number;
  effectiveFrom: string; // ISO 8601
  effectiveTo: string | null; // ISO 8601 or null if still active
  isActive: boolean;
  createdAt: string; // ISO 8601
}

export interface CreateCostConfigVersionRequest {
  currency?: string;
  electricityCostPerKwh: number;
  waterCostPerLiter: number;
  nutrientCostPerLiter: number;
  effectiveFrom?: string; // ISO 8601
  effectiveTo?: string; // ISO 8601, optional — auto-calculated if omitted
}

export interface UpdateCostConfigVersionRequest {
  currency?: string;
  electricityCostPerKwh: number;
  waterCostPerLiter: number;
  nutrientCostPerLiter: number;
  effectiveFrom?: string; // ISO 8601
  effectiveTo?: string; // ISO 8601, optional
}

// ==================== Consumption Entry Types ====================

/**
 * Consumption types:
 * 1 = ElectricityKwh
 * 2 = WaterLiters
 * 3 = NutrientLiters
 */
export type ConsumptionType = 1 | 2 | 3;

export const CONSUMPTION_TYPE_LABELS: Record<ConsumptionType, string> = {
  1: 'Electricidad (kWh)',
  2: 'Agua (Litros)',
  3: 'Nutrientes (Litros)',
};

export const CONSUMPTION_TYPE_UNITS: Record<ConsumptionType, string> = {
  1: 'kWh',
  2: 'L',
  3: 'L',
};

export const CONSUMPTION_TYPE_ICONS: Record<ConsumptionType, string> = {
  1: 'bolt',
  2: 'droplet',
  3: 'plant',
};

export interface ManualConsumptionEntry {
  id: string;
  dateFrom: string; // ISO 8601
  dateTo: string; // ISO 8601
  type: ConsumptionType;
  amount: number;
  unitCostSnapshot: number;
  currencySnapshot: string;
  costConfigVersionId: string;
  costAmount: number;
  note: string | null;
  createdAt: string; // ISO 8601
}

export interface CreateManualConsumptionEntryRequest {
  dateFrom: string; // ISO 8601
  dateTo?: string; // ISO 8601, optional — defaults to dateFrom
  type: ConsumptionType;
  amount: number;
  note?: string;
}

// ==================== Consumption Summary Types ====================

export interface BiSummary {
  from: string; // ISO 8601
  to: string; // ISO 8601
  currency: string;
  totalElectricityKwh: number;
  totalWaterLiters: number;
  totalNutrientLiters: number;
  costElectricity: number;
  costWater: number;
  costNutrients: number;
  costTotal: number;
}

// ==================== Production Record Types ====================

export interface ProductionRecord {
  id: string;
  cropName: string;
  startDate: string; // ISO 8601
  harvestDate: string; // ISO 8601
  kilosProduced: number;
  pricePerKilo: number;
  currency: string;
  note: string | null;
  createdAt: string; // ISO 8601
}

export interface CreateProductionRecordRequest {
  cropName: string;
  startDate: string; // ISO 8601
  harvestDate: string; // ISO 8601
  kilosProduced: number;
  pricePerKilo: number;
  currency?: string;
  note?: string;
}

// ==================== Operational Cost Types ====================

export interface OperationalCostRequest {
  from: string; // ISO 8601
  to: string; // ISO 8601
}

export interface CostConfigPeriodUsed {
  costConfigVersionId: string;
  periodStart: string; // ISO 8601
  periodEnd: string; // ISO 8601
  electricityCostPerKwh: number;
}

export interface ActuatorOperationalCostItem {
  actuatorCode: string;
  powerConsumptionWatts: number;
  totalDurationSeconds: number;
  totalHours: number;
  estimatedKwh: number;
  estimatedCost: number;
  activationCount: number;
}

export interface CostConfigPeriodCost {
  costConfigVersionId: string;
  periodStart: string;
  periodEnd: string;
  proportionalKwh: number;
  costPerKwh: number;
  cost: number;
}

export interface OperationalCostResponse {
  from: string;
  to: string;
  currency: string;
  costConfigPeriodsUsed: CostConfigPeriodUsed[];
  actuators: ActuatorOperationalCostItem[];
  totalEstimatedKwh: number;
  totalOperationalCost: number;
}

// ==================== Profitability Types ====================

export interface ProfitabilityRequest {
  productionRecordId: string;
  /** Costo de inversión inicial en infraestructura y hardware (opcional). Cuando se provee, la respuesta incluye roiPercent. */
  initialInvestmentCost?: number;
  /** When false, BFF skips actuator-service calls. Use when electricity was already entered manually. Default: true. */
  includeAutomaticEnergyCalculation?: boolean;
}

export interface ProductionInfo {
  productionRecordId: string;
  cropName: string;
  startDate: string;
  harvestDate: string;
  kilosProduced: number;
  pricePerKilo: number;
  currency: string;
}

export interface OperationalCostDetail {
  actuators: ActuatorOperationalCostItem[];
  totalEstimatedKwh: number;
  totalOperationalCost: number;
}

export interface ManualConsumptionCostDetail {
  totalElectricityKwh: number;
  totalWaterLiters: number;
  totalNutrientLiters: number;
  costElectricity: number;
  costWater: number;
  costNutrients: number;
  totalManualCost: number;
  entries: ManualConsumptionEntryItem[];
}

export interface ManualConsumptionEntryItem {
  type: ConsumptionType;
  amount: number;
  costAmount: number;
  note: string | null;
  dateFrom: string; // ISO 8601
  dateTo: string; // ISO 8601
}

export interface ExpensesInfo {
  operationalCost: OperationalCostDetail;
  manualConsumptionCost: ManualConsumptionCostDetail;
  totalExpenses: number;
}

export interface RevenueInfo {
  kilosProduced: number;
  pricePerKilo: number;
  totalRevenue: number;
}

export interface ProfitabilityResponse {
  production: ProductionInfo;
  expenses: ExpensesInfo;
  revenue: RevenueInfo;
  netBenefit: number;
  /** Margen de ganancia (%) = netBenefit / totalRevenue × 100 */
  profitMarginPercent: number;
  /** ROI real (%) = netBenefit / (initialInvestmentCost + totalExpenses) × 100. Solo presente si se envió initialInvestmentCost. */
  roiPercent?: number;
  /** Costo de producción por kilogramo (totalExpenses / kilosProduced) */
  costPerKiloProduced: number;
  /** Huella hídrica aproximada (L/kg) = totalWaterLiters / kilosProduced. Solo presente si hay agua registrada. */
  waterFootprintLitersPerKg?: number;
  currency: string;
}
