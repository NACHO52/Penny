import type { es } from "./locales/es";

export type Movement = {
  id: number;
  movementDate: string;
  description: string;
  categoryId: number | null;
  subcategoryId: number | null;
  type: keyof typeof es.movement.types;
  amount: number;
  exactAmount: string;
};
