<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref } from "vue";
import type { Account } from "../account";
import { es } from "../locales/es";

type Category = { id: number; name: string; subcategories: { id: number; name: string }[] };
type MovementType = keyof typeof es.movement.types;

const props = defineProps<{ accounts: Account[] }>();
const emit = defineEmits<{ saved: [accountId: number]; "open-categories": []; "session-expired": [] }>();
const text = es.movement;
const dialog = ref<HTMLDialogElement>();
const amountInput = ref<HTMLInputElement>();
const cancelButton = ref<HTMLButtonElement>();
const accountId = ref<number>();
const type = ref<MovementType>(2);
const movementTypes = [2, 1] as const;
const amount = ref("");
const description = ref("");
const movementDate = ref("");
const categoryId = ref<number>();
const subcategoryId = ref<number | null>(null);
const categories = ref<Category[]>([]);
const selectedAccount = computed(() => props.accounts.find((account) => account.id === accountId.value));
const selectedCategory = computed(() => categories.value.find((category) => category.id === categoryId.value));
const loading = ref(false);
const saving = ref(false);
const loadError = ref(false);
const uncertain = ref(false);
const saveError = ref("");
let categoryRequest: AbortController | undefined;

defineExpose({ open });
onBeforeUnmount(cancelCategoryRequest);

function cancelCategoryRequest() {
  categoryRequest?.abort();
}

function open(selectedAccountId: number) {
  if (dialog.value?.open || !props.accounts.some((account) => account.id === selectedAccountId)) return;

  accountId.value = selectedAccountId;
  type.value = 2;
  amount.value = "";
  description.value = "";
  const today = new Date();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  movementDate.value = today.getFullYear() + "-" + month + "-" + day;
  categories.value = [];
  categoryId.value = undefined;
  subcategoryId.value = null;
  uncertain.value = false;
  saveError.value = "";
  dialog.value?.showModal();
  void loadCategories();
}

async function loadCategories() {
  cancelCategoryRequest();
  const request = new AbortController();
  categoryRequest = request;
  loading.value = true;
  loadError.value = false;
  try {
    const response = await fetch("/category", { cache: "no-store", signal: request.signal });
    if (request.signal.aborted) return;
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (!response.ok) throw new Error();

    const result: Category[] = await response.json();
    if (request.signal.aborted) return;
    categories.value = result;
    if (!selectedCategory.value) categoryId.value = undefined;
    if (!selectedCategory.value?.subcategories.some((item) => item.id === subcategoryId.value)) {
      subcategoryId.value = null;
    }
  } catch {
    if (!request.signal.aborted) loadError.value = true;
  } finally {
    if (!request.signal.aborted) loading.value = false;
  }
}

async function saveMovement() {
  const account = selectedAccount.value;
  const category = selectedCategory.value;
  if (!account || !category || loading.value || loadError.value || saving.value || uncertain.value) return;

  const normalizedAmount = amount.value.replace(",", ".");
  if (!(Number(normalizedAmount) > 0)) {
    saveError.value = text.invalidAmount;
    amountInput.value?.focus();
    return;
  }

  saving.value = true;
  saveError.value = "";
  try {
    const response = await fetch("/movement", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        accountId: account.id,
        type: type.value,
        movementDate: movementDate.value,
        categoryId: category.id,
        subcategoryId: subcategoryId.value,
        amount: normalizedAmount,
        description: description.value.trim(),
      }),
    });
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (response.status === 400 || response.status === 404) {
      saveError.value = text.rejected;
      await loadCategories();
      return;
    }
    if (response.status !== 201) throw new Error();

    dialog.value?.close();
    emit("saved", account.id);
  } catch {
    uncertain.value = true;
    saveError.value = text.uncertain;
  } finally {
    saving.value = false;
    if (uncertain.value) {
      await nextTick();
      cancelButton.value?.focus();
    }
  }
}

function openCategories() {
  dialog.value?.close();
  emit("open-categories");
}
</script>

<template>
  <dialog
    ref="dialog"
    class="app-dialog new-movement-dialog"
    aria-labelledby="new-movement-title"
    @cancel="saving && $event.preventDefault()"
    @close="cancelCategoryRequest"
  >
    <header class="new-movement-heading">
      <h1 id="new-movement-title">{{ text.newTitle }}</h1>
      <button type="button" :aria-label="text.close" :disabled="saving" @click="dialog?.close()">×</button>
    </header>
    <form class="auth-form" @submit.prevent="saveMovement">
      <fieldset class="movement-types" :disabled="saving || uncertain">
        <legend>{{ text.type }}</legend>
        <label v-for="code in movementTypes" :key="code">
          <input v-model="type" type="radio" name="type" :value="code" :autofocus="code === 2" required />
          {{ text.types[code] }}
        </label>
      </fieldset>

      <p v-if="loading" role="status">{{ text.loadingCategories }}</p>
      <div v-else-if="loadError">
        <p class="auth-error" role="alert">{{ es.category.loadError }}</p>
        <button type="button" @click="loadCategories">{{ es.category.retry }}</button>
      </div>
      <div v-else-if="!categories.length">
        <p>{{ text.noCategories }}</p>
        <button type="button" @click="openCategories">{{ text.openCategories }}</button>
      </div>
      <div v-else class="new-movement-fields">
        <div class="auth-field">
          <label for="movement-category">{{ text.category }}</label>
          <select id="movement-category" v-model="categoryId" name="categoryId" required :disabled="saving || uncertain" @change="subcategoryId = null">
            <option :value="undefined" disabled>{{ text.selectCategory }}</option>
            <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
          </select>
        </div>
        <div class="auth-field">
          <label for="movement-subcategory">{{ text.subcategory }}</label>
          <select id="movement-subcategory" v-model="subcategoryId" name="subcategoryId" :disabled="!selectedCategory?.subcategories.length || saving || uncertain">
            <option :value="null">{{ text.noSubcategory }}</option>
            <option v-for="subcategory in selectedCategory?.subcategories" :key="subcategory.id" :value="subcategory.id">{{ subcategory.name }}</option>
          </select>
        </div>
      </div>
      <div class="auth-field">
        <label for="movement-date">{{ text.date }}</label>
        <input id="movement-date" v-model="movementDate" name="movementDate" type="date" required :readonly="saving || uncertain" />
      </div>
      <div class="auth-field">
        <label for="movement-description">{{ text.description }}</label>
        <input id="movement-description" v-model="description" name="description" pattern=".*\S.*" required :readonly="saving || uncertain" />
      </div>
      <div class="auth-field">
        <label for="movement-amount">{{ text.amount }}</label>
        <div class="new-movement-amount">
          <span v-if="selectedAccount" aria-hidden="true">{{ es.account.currencies[selectedAccount.currencyCode] }}</span>
          <input
            id="movement-amount"
            ref="amountInput"
            v-model="amount"
            name="amount"
            inputmode="decimal"
            pattern="[0-9]{1,16}([,.][0-9]{1,2})?"
            placeholder="0,00"
            aria-describedby="movement-amount-hint"
            required
            :readonly="saving || uncertain"
          />
        </div>
        <small id="movement-amount-hint">{{ text.amountHint }}</small>
      </div>
      <p v-if="saveError" class="auth-error" role="alert">{{ saveError }}</p>
      <footer class="app-dialog-actions">
        <button ref="cancelButton" type="button" :disabled="saving" @click="dialog?.close()">{{ uncertain ? text.close : text.cancel }}</button>
        <button class="auth-submit" type="submit" :disabled="saving || loading || loadError || !categories.length || uncertain">
          {{ saving ? text.saving : text.save }}
        </button>
      </footer>
    </form>
  </dialog>
</template>
