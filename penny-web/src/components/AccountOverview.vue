<script setup lang="ts">
import { onMounted, ref } from "vue";
import { es } from "../locales/es";
import { formatCurrency, type Account, type CurrencyCode } from "../account";

defineProps<{ showOverview: boolean }>();
const emit = defineEmits<{ "session-expired": [] }>();
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

onMounted(loadAccounts);

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
          <dt>{{ text.initialBalance }}</dt>
          <dd>{{ formatCurrency(account.initialBalance, account.currencyCode) }}</dd>
        </dl>
        <h3>{{ account.name }}</h3>
      </div>
    </template>
  </section>
  <slot v-else :accounts="accounts" :loading="loading" :load-error="loadError" :retry="loadAccounts" />
</template>
