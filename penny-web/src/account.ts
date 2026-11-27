/// <reference lib="es2023.intl" />
import { es } from "./locales/es";

export type CurrencyCode = keyof typeof es.account.currencies;

export type Account = {
  id: number;
  name: string;
  currencyCode: CurrencyCode;
  initialBalance: number;
  currentBalance: `${number}`;
};

export function formatCurrency(amount: number | `${number}`, currencyCode: CurrencyCode) {
  return new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: es.account.currencies[currencyCode],
    currencyDisplay: "code",
  }).format(amount);
}
