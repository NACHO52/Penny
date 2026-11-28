<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { Account } from "../account";
import { es } from "../locales/es";

type ExpenseRow = {
  rowKind: 0 | 1 | 2 | 3;
  categoryId: number | null;
  subcategoryId: number | null;
  name: string | null;
  monthlyAmounts: `${number}`[];
  annualTotal: `${number}`;
  annualShare: number | null;
};

type ExpenseReport = {
  firstYear: number | null;
  lastYear: number | null;
  rows: ExpenseRow[];
};

const props = defineProps<{ accounts: Account[]; accountId: number; year: number }>();
const emit = defineEmits<{
  "filters-selected": [accountId: number, year: number];
  "session-expired": [];
}>();

const text = es.report;
const report = ref<ExpenseReport>();
const yearRange = ref<{ accountId: number; first: number | null; last: number | null }>();
const loading = ref(true);
const error = ref(false);
const retry = ref(0);
const amountFormat = new Intl.NumberFormat("es-AR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const percentFormat = new Intl.NumberFormat("es-AR", { style: "percent", maximumFractionDigits: 2 });
const monthFormat = new Intl.DateTimeFormat("es-AR", { month: "long", timeZone: "UTC" });
const shortMonthFormat = new Intl.DateTimeFormat("es-AR", { month: "short", timeZone: "UTC" });
const months = Array.from({ length: 12 }, (_, month) => {
  const date = Date.UTC(2024, month, 1);
  return { name: monthFormat.format(date), shortName: shortMonthFormat.format(date) };
});
const account = computed(() => props.accounts.find((item) => item.id === props.accountId));

const years = computed(() => {
  const currentYear = new Date().getFullYear();
  const available = new Set([currentYear, props.year]);
  const range = yearRange.value;
  if (range?.accountId === props.accountId && range.first !== null && range.last !== null) {
    for (let year = range.first; year <= range.last; year++) available.add(year);
  }
  return [...available].sort((left, right) => right - left);
});

const categoryNames = computed(() => new Map(report.value?.rows
  .filter((row) => row.rowKind === 0)
  .map((row) => [row.categoryId, row.name])));
const details = computed(() => report.value?.rows.filter((row) => row.rowKind !== 3) ?? []);
const total = computed(() => report.value?.rows.find((row) => row.rowKind === 3));

watch([() => props.accountId, () => props.year, retry], async ([accountId, year], _, onCleanup) => {
  const controller = new AbortController();
  onCleanup(() => controller.abort());
  loading.value = true;
  error.value = false;
  report.value = undefined;

  try {
    const response = await fetch(`/report/monthly-expenses?year=${year}&accountId=${accountId}`, {
      cache: "no-store",
      signal: controller.signal,
    });
    if (controller.signal.aborted) return;
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (!response.ok) throw new Error();

    const result: ExpenseReport = await response.json();
    if (controller.signal.aborted) return;
    report.value = result;
    yearRange.value = { accountId, first: result.firstYear, last: result.lastYear };
  } catch {
    if (!controller.signal.aborted) error.value = true;
  } finally {
    if (!controller.signal.aborted) loading.value = false;
  }
}, { immediate: true });
</script>

<template>
  <section class="expense-report-panel" aria-label="Filtros y gastos mensuales">
    <div class="expense-report-filters">
      <div class="auth-field">
        <label for="report-year">{{ text.year }}</label>
        <select
          id="report-year"
          :value="year"
          @change="emit('filters-selected', accountId, Number(($event.target as HTMLSelectElement).value))"
        >
          <option v-for="option in years" :key="option" :value="option">{{ option }}</option>
        </select>
      </div>
      <div class="auth-field">
        <label for="report-account">{{ text.account }}</label>
        <select
          id="report-account"
          :value="accountId"
          @change="emit('filters-selected', Number(($event.target as HTMLSelectElement).value), year)"
        >
          <option v-for="option in accounts" :key="option.id" :value="option.id">{{ option.name }}</option>
        </select>
      </div>
      <span v-if="account" class="expense-report-currency">
        {{ es.account.currencies[account.currencyCode] }}
      </span>
    </div>

    <p v-if="loading" class="expense-report-status" role="status">{{ text.loading }}</p>
    <div v-else-if="error" class="expense-report-status">
      <p class="auth-error" role="alert">{{ text.loadError }}</p>
      <button class="auth-submit" type="button" @click="retry++">{{ text.retry }}</button>
    </div>
    <div
      v-else-if="account && total"
      class="expense-report-table"
      tabindex="0"
      role="region"
      aria-labelledby="expense-report-caption"
    >
      <table>
        <caption id="expense-report-caption" class="expense-report-sr-only">
          {{ text.caption(account.name, year, es.account.currencies[account.currencyCode]) }}
        </caption>
        <thead>
          <tr>
            <th scope="col">{{ text.categories }}</th>
            <th v-for="month in months" :key="month.name" scope="col" :aria-label="month.name">
              <span class="expense-report-month-name">{{ month.name }}</span>
              <abbr class="expense-report-month-short" :title="month.name">{{ month.shortName }}</abbr>
            </th>
            <th scope="col" class="expense-report-annual-start">{{ text.annualTotal }}</th>
            <th scope="col" class="expense-report-annual">{{ text.annualShare }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="row in details"
            :key="`${row.rowKind}-${row.categoryId}-${row.subcategoryId}`"
            :class="row.rowKind === 0 ? 'expense-report-category' : 'expense-report-subcategory'"
          >
            <th scope="row">
              <span v-if="row.rowKind !== 0" class="expense-report-sr-only">
                {{ categoryNames.get(row.categoryId) }} —
              </span>
              {{ row.rowKind === 2 ? es.movement.otherSubcategory : row.name }}
            </th>
            <td v-for="(amount, month) in row.monthlyAmounts" :key="month">{{ amountFormat.format(amount) }}</td>
            <td class="expense-report-annual-start">{{ amountFormat.format(row.annualTotal) }}</td>
            <td class="expense-report-annual">{{ row.annualShare === null ? '—' : percentFormat.format(row.annualShare) }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <th scope="row">{{ text.monthlyTotals }}</th>
            <td v-for="(amount, month) in total.monthlyAmounts" :key="month">{{ amountFormat.format(amount) }}</td>
            <td class="expense-report-annual-start">{{ amountFormat.format(total.annualTotal) }}</td>
            <td class="expense-report-annual">{{ total.annualShare === null ? '—' : percentFormat.format(total.annualShare) }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  </section>
</template>
