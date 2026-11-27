<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from "vue";
import { es } from "../locales/es";
import { useFieldValidation } from "../fieldValidation";
import { formatCurrency, type Account, type CurrencyCode } from "../account";

const props = defineProps<{ showOverview: boolean; movementRevision: number }>();
const emit = defineEmits<{
  "session-expired": [];
  "accounts-changed": [Account[] | null];
  "new-movement": [accountId: number];
}>();
const text = es.account;
const accounts = ref<Account[] | null>(null);
const loading = ref(true);
const saving = ref(false);
const loadError = ref("");
const saveError = ref("");
const status = ref("");
const name = ref("");
const currencyCode = ref<CurrencyCode>(1);
const initialBalance = ref("");
const renameDialog = ref<HTMLDialogElement>();
const renameAccountId = ref<number>();
const renameName = ref("");
const renameError = ref("");
const renameValidation = useFieldValidation({ "rename-account-name": es.validation.name });
const renaming = ref(false);

onMounted(loadAccounts);
watch(() => props.movementRevision, loadAccounts);
watch(accounts, (value) => emit("accounts-changed", value));

async function loadAccounts() {
  loading.value = true;
  loadError.value = "";
  try {
    const response = await fetch("/account", { cache: "no-store" });
    if (response.status === 401) {
      emit("session-expired");
    } else if (response.ok) {
      accounts.value = await response.json();
    } else {
      loadError.value = text.loadError;
    }
  } catch {
    loadError.value = text.loadError;
  } finally {
    loading.value = false;
  }
}

async function createAccount() {
  if (saving.value || accounts.value?.length !== 0 || loadError.value) return;

  saving.value = true;
  saveError.value = "";
  try {
    const response = await fetch("/account", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: name.value.trim(),
        currencyCode: currencyCode.value,
        initialBalance: initialBalance.value.replace(",", "."),
      }),
    });

    if (response.status === 401) {
      emit("session-expired");
    } else if (response.status === 400) {
      saveError.value = text.rejected;
    } else if (response.status === 409) {
      loadError.value = text.duplicateName;
    } else if (response.status === 201) {
      accounts.value = [await response.json()];
      status.value = text.created;
    } else {
      loadError.value = text.createError;
    }
  } catch {
    loadError.value = text.createError;
  } finally {
    saving.value = false;
  }
}

async function openRename(account: Account) {
  if (renaming.value || renameDialog.value?.open) return;

  renameAccountId.value = account.id;
  renameName.value = account.name;
  renameError.value = "";
  renameValidation.reset();
  status.value = "";
  await nextTick();
  renameDialog.value?.showModal();
}

async function renameAccount() {
  const accountId = renameAccountId.value;
  const newName = renameName.value.trim();
  if (accountId === undefined || !newName || renaming.value) return;

  renaming.value = true;
  renameError.value = "";
  try {
    const response = await fetch("/account/" + accountId, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: newName }),
    });
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (response.status === 409) {
      renameValidation.errors["rename-account-name"] = text.duplicateName;
      return;
    }
    if (response.status === 404) {
      renameError.value = text.accountUnavailable;
      return;
    }
    if (response.status !== 204) throw new Error();

    accounts.value = accounts.value?.map((account) =>
      account.id === accountId ? { ...account, name: newName } : account,
    ) ?? null;
    status.value = text.renamed;
    renameDialog.value?.close();
  } catch {
    renameError.value = text.renameError;
  } finally {
    renaming.value = false;
  }
}
</script>

<template>
  <section
    v-if="showOverview"
    :class="accounts?.length && !loadError && !loading ? 'account-overview' : 'account-panel'"
    aria-labelledby="account-title"
  >
    <h2 id="account-title">{{ accounts?.length === 0 && !loadError && !loading ? text.createTitle : text.title }}</h2>
    <p role="status">{{ loading ? text.loading : saving ? text.creating : status }}</p>

    <div v-if="loadError">
      <p class="auth-error" role="alert">{{ loadError }}</p>
      <button class="auth-submit" type="button" @click="loadAccounts">{{ text.retry }}</button>
    </div>

    <template v-else-if="!loading && accounts">
      <form v-if="accounts.length === 0" class="auth-form" action="/account" method="post" @submit.prevent="createAccount">
        <div class="auth-field">
          <label for="account-name">{{ text.name }}</label>
          <input id="account-name" v-model="name" name="name" pattern=".*\S.*" required :readonly="saving" />
        </div>

        <fieldset class="account-currencies" :disabled="saving">
          <legend>{{ text.currency }}</legend>
          <label v-for="(currency, code) in text.currencies" :key="code" :for="'currency-' + code">
            <input :id="'currency-' + code" v-model="currencyCode" type="radio" name="currencyCode" :value="code" required />
            {{ currency }}
          </label>
        </fieldset>

        <div class="auth-field">
          <label for="initial-balance">{{ text.initialBalance }}</label>
          <small id="balance-hint">{{ text.balanceHint }}</small>
          <input
            id="initial-balance"
            v-model="initialBalance"
            name="initialBalance"
            inputmode="decimal"
            pattern="-?[0-9]{1,16}([,.][0-9]{1,2})?"
            aria-describedby="balance-hint"
            required
            :readonly="saving"
          />
        </div>

        <p v-if="saveError" class="auth-error" role="alert">{{ saveError }}</p>
        <button class="auth-submit" type="submit" :disabled="saving">{{ text.create }}</button>
      </form>

      <div v-for="account in accounts" :key="account.id" class="account-summary">
        <dl>
          <dt>{{ text.currentBalance }}</dt>
          <dd>{{ formatCurrency(account.currentBalance, account.currencyCode) }}</dd>
        </dl>
        <h3>{{ account.name }}</h3>
        <div class="account-actions">
          <button
            class="account-new-movement"
            type="button"
            :aria-label="es.movement.newForAccount(account.name)"
            @click="emit('new-movement', account.id)"
          >
            <span aria-hidden="true">+</span> {{ es.home.newMovement }}
          </button>
          <button
            class="account-rename"
            type="button"
            :aria-label="text.editNamed(account.name)"
            @click="openRename(account)"
          >
            {{ text.editName }}
          </button>
        </div>
      </div>
    </template>
    <dialog
      ref="renameDialog"
      class="app-dialog"
      aria-labelledby="rename-account-title"
      @cancel="renaming && $event.preventDefault()"
    >
      <h1 id="rename-account-title">{{ text.editName }}</h1>
      <form class="auth-form" @submit.prevent="renameAccount" @invalid.capture.prevent="renameValidation.invalid" @input="renameValidation.clearValidError">
        <div class="auth-field">
          <label for="rename-account-name">{{ text.name }}</label>
          <input
            id="rename-account-name"
            v-model="renameName"
            name="name"
            pattern=".*\S.*"
            required
            autofocus
            :readonly="renaming"
            v-bind="renameValidation.attributes('rename-account-name')"
          />
          <p v-if="renameValidation.errors['rename-account-name']" id="rename-account-name-error" class="field-error" role="alert">
            {{ renameValidation.errors["rename-account-name"] }}
          </p>
        </div>
        <p v-if="renameError" class="auth-error" role="alert">{{ renameError }}</p>
        <div class="app-dialog-actions">
          <button type="button" :disabled="renaming" @click="renameDialog?.close()">{{ text.cancel }}</button>
          <button class="auth-submit" type="submit" :disabled="renaming">{{ renaming ? text.renaming : text.saveName }}</button>
        </div>
      </form>
    </dialog>
  </section>
  <slot v-else :accounts="accounts" :loading="loading" :load-error="loadError" :retry="loadAccounts" />
</template>
