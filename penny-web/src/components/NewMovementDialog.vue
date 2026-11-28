<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref } from "vue";
import type { Account } from "../account";
import { es } from "../locales/es";
import { useFieldValidation } from "../fieldValidation";
import type { Movement } from "../movement";

type Category = { id: number; name: string; subcategories: { id: number; name: string }[] };
type MovementType = keyof typeof es.movement.types;

const props = defineProps<{ accounts: Account[] }>();
const emit = defineEmits<{ saved: [accountId: number, editing: boolean]; "open-categories": []; "session-expired": [] }>();
const text = es.movement;
const validation = useFieldValidation({
  "movement-amount": text.invalidAmount,
  "movement-description": text.invalidDescription,
});
const dialog = ref<HTMLDialogElement>();
const amountInput = ref<HTMLInputElement>();
const cancelButton = ref<HTMLButtonElement>();
const accountId = ref<number>();
const movementId = ref<number>();
const editing = computed(() => movementId.value !== undefined);
const type = ref<MovementType>(2);
const movementTypes = [2, 1] as const;
const isExpense = computed(() => type.value === 2);
const amount = ref("");
const description = ref("");
const movementDate = ref("");
const categoryId = ref<number | "">("");
const categoryInput = ref<HTMLSelectElement>();
const subcategoryId = ref<number | "" | "other">("");
const subcategoryInput = ref<HTMLSelectElement>();
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

function handleAmountSeparator(event: InputEvent) {
  const input = event.currentTarget as HTMLInputElement;
  const start = input.selectionStart ?? 0;
  const end = input.selectionEnd ?? start;
  const comma = input.value.indexOf(",");
  if (!event.cancelable || event.isComposing || start !== end || comma < 0) return;

  if (event.inputType === "insertText" && (event.data === "," || event.data === ".")) {
    event.preventDefault();
    input.setSelectionRange(comma + 1, comma + 1);
  } else if (event.inputType === "deleteContentBackward" && start === comma + 1) {
    event.preventDefault();
    input.setSelectionRange(comma, comma);
  } else if (event.inputType === "deleteContentForward" && start === comma) {
    event.preventDefault();
    input.setSelectionRange(comma + 1, comma + 1);
  } else if (event.inputType === "deleteContentBackward" && input.value[start - 1] === ".") {
    input.setSelectionRange(start - 1, start - 1);
  } else if (event.inputType === "deleteContentForward" && input.value[start] === ".") {
    input.setSelectionRange(start + 1, start + 1);
  }
}

function formatAmount(event: InputEvent) {
  if (event.isComposing) return;

  const input = event.currentTarget as HTMLInputElement;
  const cursor = input.selectionStart ?? input.value.length;
  const pasted = event.inputType === "insertFromPaste";
  const deleting = event.inputType.startsWith("delete");
  let raw = input.value;
  if (raw === "." || (pasted && /^\d+\.\d{1,2}$/.test(raw))) {
    raw = raw.replace(".", ",");
  }
  if (raw && !raw.includes(",") && deleting && amount.value.includes(",")) {
    // Preserve the decimal boundary when a selection spanning the comma is deleted.
    raw = raw.slice(0, cursor) + "," + raw.slice(cursor);
  }

  const comma = raw.indexOf(",");
  const whole = (comma < 0 ? raw : raw.slice(0, comma)).replace(/\./g, "");
  const fraction = comma < 0 ? "" : raw.slice(comma + 1).replace(/^,/, "");
  if (!/^\d{0,16}$/.test(whole) || !/^\d*$/.test(fraction) || (pasted && fraction.length > 2)) {
    input.value = amount.value;
    input.setSelectionRange(cursor, cursor);
    return;
  }

  if (!raw || (deleting && !whole && !/[1-9]/.test(fraction))) {
    input.value = "";
    amount.value = "";
    return;
  }

  const integer = whole.replace(/^0+(?=\d)/, "") || "0";
  const grouped = integer.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  const formatted = grouped + "," + fraction.slice(0, 2).padEnd(2, "0");

  let position: number;
  if (comma >= 0 && cursor > comma) {
    const centsBeforeCursor = raw.slice(comma + 1, cursor).replace(/\D/g, "").length;
    position = grouped.length + 1 + Math.min(centsBeforeCursor, 2);
  } else {
    let digits = raw.slice(0, cursor).replace(/\D/g, "").length - Math.max(0, whole.length - integer.length);
    position = 0;
    while (digits > 0 && position < grouped.length) {
      if (grouped[position] !== ".") digits--;
      position++;
    }
  }

  amount.value = formatted;
  input.value = formatted;
  input.setSelectionRange(position, position);
}

async function open(selectedAccountId: number, movement?: Movement) {
  if (dialog.value?.open || !props.accounts.some((account) => account.id === selectedAccountId)) return;

  accountId.value = selectedAccountId;
  movementId.value = movement?.id;
  type.value = movement?.type ?? 2;
  const [integer, cents] = movement?.exactAmount.split(".") ?? [];
  amount.value = integer === undefined ? "" : integer.replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "," + cents;
  description.value = movement?.description ?? "";
  const today = new Date();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  movementDate.value = movement?.movementDate ?? today.getFullYear() + "-" + month + "-" + day;
  categories.value = [];
  categoryId.value = movement?.categoryId ?? "";
  subcategoryId.value = movement?.type === 2 ? movement.subcategoryId ?? "other" : "";
  uncertain.value = false;
  saveError.value = "";
  validation.reset();
  void loadCategories();
  await nextTick();
  dialog.value?.showModal();
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
    if (!selectedCategory.value) categoryId.value = "";
    if (!selectedCategory.value || (typeof subcategoryId.value === "number" &&
        !selectedCategory.value.subcategories.some((item) => item.id === subcategoryId.value))) {
      subcategoryId.value = "";
    }
  } catch {
    if (!request.signal.aborted) loadError.value = true;
  } finally {
    if (!request.signal.aborted) loading.value = false;
  }
}

async function saveMovement(event: SubmitEvent) {
  const account = selectedAccount.value;
  if (!account || saving.value || uncertain.value) return;
  const category = isExpense.value ? selectedCategory.value : undefined;
  if (isExpense.value && (loading.value || loadError.value)) return;

  const form = event.currentTarget as HTMLFormElement;
  const valid = form.checkValidity();
  if (isExpense.value && !category) {
    validation.errors["movement-category"] = es.validation.required;
    await nextTick();
    categoryInput.value?.focus();
    return;
  }
  if (isExpense.value && subcategoryId.value === "") {
    validation.errors["movement-subcategory"] = text.subcategoryRequired;
    await nextTick();
    subcategoryInput.value?.focus();
    return;
  }
  if (!valid) return;

  const normalizedAmount = amount.value.replace(/\./g, "").replace(",", ".");
  if (!(Number(normalizedAmount) > 0)) {
    validation.errors["movement-amount"] = text.invalidAmount;
    amountInput.value?.focus();
    return;
  }

  saving.value = true;
  saveError.value = "";
  try {
    const response = await fetch(editing.value ? "/movement/" + movementId.value : "/movement", {
      method: editing.value ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        accountId: account.id,
        type: type.value,
        movementDate: movementDate.value,
        categoryId: category?.id ?? null,
        subcategoryId: category && typeof subcategoryId.value === "number" ? subcategoryId.value : null,
        amount: normalizedAmount,
        description: description.value.trim(),
      }),
    });
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (editing.value && response.status === 404) {
      saveError.value = text.movementUnavailable;
      return;
    }
    if (response.status === 400 || response.status === 404) {
      const failure: { code?: string } | null = await response.json().catch(() => null);
      if (isExpense.value) await loadCategories();
      if (categories.value.length && !loadError.value && failure?.code === "invalid_category") {
        validation.errors["movement-category"] = text.invalidCategory;
      } else if (selectedCategory.value && !loadError.value && failure?.code === "invalid_subcategory") {
        validation.errors["movement-subcategory"] = text.invalidSubcategory;
      } else {
        saveError.value = text.rejected;
      }
      return;
    }
    if (response.status !== (editing.value ? 200 : 201)) throw new Error();

    dialog.value?.close();
    emit("saved", account.id, editing.value);
  } catch {
    uncertain.value = !editing.value;
    saveError.value = editing.value ? text.updateError : text.uncertain;
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

function changeCategory() {
  subcategoryId.value = "";
  delete validation.errors["movement-subcategory"];
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
      <h1 id="new-movement-title">{{ editing ? text.editTitle : text.newTitle }}</h1>
      <button type="button" :aria-label="text.close" :disabled="saving" @click="dialog?.close()">×</button>
    </header>
    <form
      class="auth-form"
      novalidate
      @submit.prevent="saveMovement"
      @invalid.capture.prevent="validation.invalid"
      @input="validation.clearValidError"
      @change="validation.clearValidError"
    >
      <fieldset class="movement-types" :disabled="saving || uncertain">
        <legend>{{ text.type }} <span aria-hidden="true">*</span></legend>
        <label v-for="code in movementTypes" :key="code">
          <input v-model="type" type="radio" name="type" :value="code" :autofocus="code === 2" required v-bind="validation.attributes('type')" />
          {{ text.types[code] }}
        </label>
        <p v-if="validation.errors.type" id="type-error" class="field-error" role="alert">{{ validation.errors.type }}</p>
      </fieldset>

      <template v-if="isExpense">
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
            <label for="movement-category">{{ text.category }} <span aria-hidden="true">*</span></label>
            <select id="movement-category" ref="categoryInput" v-model="categoryId" name="categoryId" required :disabled="saving || uncertain" @change="changeCategory" v-bind="validation.attributes('movement-category')">
              <option value="" disabled>{{ text.selectCategory }}</option>
              <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
            </select>
            <p v-if="validation.errors['movement-category']" id="movement-category-error" class="field-error" role="alert">
              {{ validation.errors["movement-category"] }}
            </p>
          </div>
          <div class="auth-field">
            <label for="movement-subcategory">{{ text.subcategory }} <span aria-hidden="true">*</span></label>
            <select id="movement-subcategory" ref="subcategoryInput" v-model="subcategoryId" name="subcategoryId" required :disabled="!selectedCategory || saving || uncertain" v-bind="validation.attributes('movement-subcategory')">
              <option value="" disabled>{{ text.selectSubcategory }}</option>
              <option v-for="subcategory in selectedCategory?.subcategories" :key="subcategory.id" :value="subcategory.id">{{ subcategory.name }}</option>
              <option value="other">{{ text.otherSubcategory }}</option>
            </select>
            <p v-if="validation.errors['movement-subcategory']" id="movement-subcategory-error" class="field-error" role="alert">
              {{ validation.errors["movement-subcategory"] }}
            </p>
          </div>
        </div>
      </template>
      <div class="auth-field">
        <label for="movement-date">{{ text.date }} <span aria-hidden="true">*</span></label>
        <input id="movement-date" v-model="movementDate" name="movementDate" type="date" required :readonly="saving || uncertain" v-bind="validation.attributes('movement-date')" />
        <p v-if="validation.errors['movement-date']" id="movement-date-error" class="field-error" role="alert">
          {{ validation.errors["movement-date"] }}
        </p>
      </div>
      <div class="auth-field">
        <label for="movement-description">{{ text.description }} <span aria-hidden="true">*</span></label>
        <input id="movement-description" v-model="description" name="description" pattern=".*\S.*" required :readonly="saving || uncertain" v-bind="validation.attributes('movement-description')" />
        <p v-if="validation.errors['movement-description']" id="movement-description-error" class="field-error" role="alert">
          {{ validation.errors["movement-description"] }}
        </p>
      </div>
      <div class="auth-field">
        <label for="movement-amount">{{ text.amount }} <span aria-hidden="true">*</span></label>
        <div class="new-movement-amount">
          <span v-if="selectedAccount" aria-hidden="true">{{ es.account.currencies[selectedAccount.currencyCode] }}</span>
          <input
            id="movement-amount"
            ref="amountInput"
            :value="amount"
            name="amount"
            inputmode="decimal"
            pattern="(?=.*[1-9])[0-9]{1,3}(\.[0-9]{3}){0,5},[0-9]{2}"
            placeholder="0,00"
            v-bind="validation.attributes('movement-amount', 'movement-amount-hint')"
            required
            :readonly="saving || uncertain"
            @beforeinput="handleAmountSeparator"
            @input="formatAmount"
          />
        </div>
        <p v-if="validation.errors['movement-amount']" id="movement-amount-error" class="field-error" role="alert">
          {{ validation.errors["movement-amount"] }}
        </p>
        <small id="movement-amount-hint">{{ text.amountHint }}</small>
      </div>
      <p v-if="saveError" class="auth-error" role="alert">{{ saveError }}</p>
      <footer class="app-dialog-actions">
        <button ref="cancelButton" type="button" :disabled="saving" @click="dialog?.close()">{{ uncertain ? text.close : text.cancel }}</button>
        <button class="auth-submit" type="submit" :disabled="saving || uncertain || (isExpense && (loading || loadError || !categories.length))">
          {{ editing ? (saving ? text.updating : text.update) : (saving ? text.saving : text.save) }}
        </button>
      </footer>
    </form>
  </dialog>
</template>
