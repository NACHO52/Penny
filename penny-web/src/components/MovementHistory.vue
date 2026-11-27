<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { formatCurrency, type Account } from "../account";
import { es } from "../locales/es";

type Movement = {
  id: number;
  movementDate: string;
  description: string;
  categoryId: number;
  subcategoryId: number | null;
  type: keyof typeof es.movement.types;
  amount: number;
};
type Category = { id: number; name: string; subcategories: { id: number; name: string }[] };

const props = defineProps<{ accounts: Account[] }>();
const emit = defineEmits<{ "session-expired": []; "new-movement": [accountId: number] }>();
const text = es.movement;
const accountId = defineModel<number | undefined>("accountId", { required: true });
const account = computed(() =>
  props.accounts.find((item) => item.id === accountId.value) ?? props.accounts[0],
);
const movements = ref<Movement[]>([]);
const categories = ref(new Map<number, Category>());
const loading = ref(true);
const error = ref(false);
const retry = ref(0);
const dateFormat = new Intl.DateTimeFormat("es-AR", { timeZone: "UTC" });

watch([account, retry], async ([selectedAccount], _, onCleanup) => {
  if (!selectedAccount) return;

  const controller = new AbortController();
  onCleanup(() => controller.abort());
  loading.value = true;
  error.value = false;
  movements.value = [];

  try {
    const options: RequestInit = {
      cache: "no-store",
      signal: controller.signal,
    };
    const [response, categoryResponse] = await Promise.all([
      fetch(`/movement?accountId=${selectedAccount.id}`, options),
      fetch("/category", options),
    ]);
    if (controller.signal.aborted) return;
    if (response.status === 401 || categoryResponse.status === 401) {
      emit("session-expired");
    } else if (response.ok && categoryResponse.ok) {
      const [result, categoryList]: [Movement[], Category[]] = await Promise.all([
        response.json(),
        categoryResponse.json(),
      ]);
      if (controller.signal.aborted) return;
      movements.value = result;
      categories.value = new Map(categoryList.map((category) => [category.id, category]));
    } else {
      error.value = true;
    }
  } catch {
    if (!controller.signal.aborted) error.value = true;
  } finally {
    if (!controller.signal.aborted) loading.value = false;
  }
}, { immediate: true });
</script>

<template>
  <div v-if="account">
    <div class="movement-toolbar">
      <div class="movement-account">
        <label for="movement-account">{{ text.account }}</label>
        <select id="movement-account" :value="account.id" @change="accountId = Number(($event.target as HTMLSelectElement).value)">
          <option v-for="item in accounts" :key="item.id" :value="item.id">{{ item.name }}</option>
        </select>
      </div>
      <button
        class="home-new-movement"
        type="button"
        :aria-label="text.newForAccount(account.name)"
        @click="emit('new-movement', account.id)"
      >
        <span aria-hidden="true">+</span> {{ text.newTitle }}
      </button>
    </div>

    <p v-if="loading" role="status">{{ text.loading }}</p>
    <template v-else-if="error">
      <p class="auth-error" role="alert">{{ text.loadError }}</p>
      <button class="auth-submit" type="button" @click="retry++">{{ text.retry }}</button>
    </template>
    <p v-else-if="movements.length === 0" role="status">{{ text.empty }}</p>
    <div v-else class="movement-table" tabindex="0" role="region" aria-labelledby="movement-caption">
      <table>
        <caption id="movement-caption">{{ text.history }}</caption>
        <thead>
          <tr>
            <th scope="col">{{ text.category }}</th>
            <th scope="col">{{ text.subcategory }}</th>
            <th scope="col">{{ text.date }}</th>
            <th scope="col">{{ text.description }}</th>
            <th scope="col">{{ text.amount }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="movement in movements" :key="movement.id">
            <td>{{ categories.get(movement.categoryId)?.name ?? "—" }}</td>
            <td>{{ categories.get(movement.categoryId)?.subcategories.find((item) => item.id === movement.subcategoryId)?.name ?? "—" }}</td>
            <td><time :datetime="movement.movementDate">{{ dateFormat.format(new Date(movement.movementDate)) }}</time></td>
            <th scope="row">{{ movement.description }}</th>
            <td :class="movement.type === 1 ? 'movement-income' : 'movement-expense'">
              {{ movement.type === 1 ? "+" : "−" }} {{ formatCurrency(movement.amount, account.currencyCode) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
