<script setup lang="ts">
import { nextTick, onMounted, onUnmounted, ref, watch } from "vue";
import AccountOverview from "./components/AccountOverview.vue";
import CategoryManager from "./components/CategoryManager.vue";
import MonthlyExpenseReport from "./components/MonthlyExpenseReport.vue";
import MovementHistory from "./components/MovementHistory.vue";
import NewMovementDialog from "./components/NewMovementDialog.vue";
import ProfileForm, { type Profile, type ProfileUpdate } from "./components/ProfileForm.vue";
import SuccessToast from "./components/SuccessToast.vue";
import type { Account } from "./account";
import { es } from "./locales/es";

const text = es.login;
const email = ref("");
const password = ref("");
const showPassword = ref(false);
const submitting = ref(false);
const error = ref("");
const currentUser = ref<Profile | null>(null);
const accounts = ref<Account[] | null>(null);
const movementAccountId = ref(readMovementAccountId());
const reportAccountId = ref<number>();
const reportYear = ref(new Date().getFullYear());
const movementDialog = ref<InstanceType<typeof NewMovementDialog>>();
const movementRevision = ref(0);
const successToast = ref<InstanceType<typeof SuccessToast>>();
const checkingSession = ref(true);
const signingOut = ref(false);
const signOutDialog = ref<HTMLDialogElement>();
const savingProfile = ref(false);
const signOutError = ref("");
const pagePaths = {
  home: "/",
  movements: "/movements",
  categories: "/categories",
  monthlyExpenses: "/reports/monthly-expenses",
  profile: "/profile",
} as const;
type Page = keyof typeof pagePaths;
const pages = Object.keys(pagePaths) as Page[];
const activePage = ref(readPage());
const reportsOpen = ref(activePage.value === "monthlyExpenses");
const pageHeading = ref<HTMLHeadingElement>();
const pageTitles = {
  home: es.home.title,
  movements: es.home.movements,
  categories: es.home.categories,
  monthlyExpenses: es.report.title,
  profile: es.home.profile,
};

function clearSession() {
  currentUser.value = null;
  accounts.value = null;
  movementAccountId.value = undefined;
  reportAccountId.value = undefined;
  reportsOpen.value = false;
  signOutError.value = "";
}

async function saveProfile(request: ProfileUpdate) {
  if (signingOut.value || savingProfile.value) return;
  const userId = currentUser.value?.id;
  savingProfile.value = true;
  try {
    const response = await fetch("/auth/me", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
    if (currentUser.value?.id === userId) {
      if (response.status === 401) clearSession();
      if (response.ok) {
        currentUser.value = await response.json() as Profile;
        showSuccess(es.profile.saved);
      }
    }
    return response;
  } finally {
    savingProfile.value = false;
  }
}

function readPage(): Page {
  const path = window.location.pathname.replace(/\/+$/, "") || "/";
  return pages.find((page) => pagePaths[page] === path) ?? "home";
}

function readMovementAccountId() {
  return readQueryInteger("accountId", 2_147_483_647);
}

function readQueryInteger(name: string, maximum: number) {
  const raw = new URLSearchParams(window.location.search).get(name);
  if (!raw || !/^[1-9]\d*$/.test(raw)) return;

  const value = Number(raw);
  return value <= maximum ? value : undefined;
}

function syncPage() {
  activePage.value = readPage();
  if (activePage.value === "movements") {
    movementAccountId.value = readMovementAccountId();
  }
  if (activePage.value === "monthlyExpenses") {
    reportAccountId.value = readQueryInteger("accountId", 2_147_483_647);
    reportYear.value = readQueryInteger("year", 9999) ?? new Date().getFullYear();
  }
}

function movementPath(accountId: number | undefined) {
  const path = readPage() === "movements" ? window.location.href : pagePaths.movements;
  const url = new URL(path, window.location.origin);
  url.pathname = pagePaths.movements;
  if (accountId === undefined) {
    url.searchParams.delete("accountId");
  } else {
    url.searchParams.set("accountId", String(accountId));
  }
  return url.pathname + url.search + url.hash;
}

function currentPath() {
  return window.location.pathname + window.location.search + window.location.hash;
}

function reportPath(accountId: number | undefined, year: number) {
  const path = readPage() === "monthlyExpenses" ? window.location.href : pagePaths.monthlyExpenses;
  const url = new URL(path, window.location.origin);
  url.pathname = pagePaths.monthlyExpenses;
  url.searchParams.set("year", String(year));
  if (accountId === undefined) {
    url.searchParams.delete("accountId");
  } else {
    url.searchParams.set("accountId", String(accountId));
  }
  return url.pathname + url.search + url.hash;
}

function selectReportFilters(accountId: number, year: number) {
  const path = reportPath(accountId, year);
  if (path === currentPath()) return;
  window.history.pushState(null, "", path);
  syncPage();
}

function selectMovementAccount(accountId: number) {
  const path = movementPath(accountId);
  if (path === currentPath()) return;

  window.history.pushState(null, "", path);
  syncPage();
}

function navigate(event: MouseEvent, page: Page) {
  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

  event.preventDefault();
  return openPage(page);
}

async function openPage(page: Page, accountId = movementAccountId.value) {
  let path: string = pagePaths[page];
  if (page === "movements") path = movementPath(accountId);
  if (page === "monthlyExpenses") path = reportPath(reportAccountId.value ?? accounts.value?.[0]?.id, reportYear.value);
  if (path === currentPath()) return;

  window.history.pushState(null, "", path);
  syncPage();
  await nextTick();
  pageHeading.value?.focus();
}

function showSuccess(message: string) {
  successToast.value?.show(message);
}

async function movementSaved(accountId: number, editing: boolean) {
  movementRevision.value++;
  await openPage("movements", accountId);
  if (editing) pageHeading.value?.focus();
  showSuccess(editing ? es.movement.updated : es.movement.created);
}

watch([accounts, activePage, movementAccountId], ([availableAccounts, page, accountId]) => {
  if (page !== "movements" || availableAccounts === null) return;

  const account = availableAccounts.find((item) => item.id === accountId) ?? availableAccounts[0];
  const path = movementPath(account?.id);
  if (path !== currentPath()) window.history.replaceState(window.history.state, "", path);
  movementAccountId.value = account?.id;
});

watch([accounts, activePage, reportAccountId, reportYear], ([availableAccounts, page, accountId, year]) => {
  if (page !== "monthlyExpenses" || availableAccounts === null) return;

  const account = availableAccounts.find((item) => item.id === accountId) ?? availableAccounts[0];
  const path = reportPath(account?.id, year);
  if (path !== currentPath()) window.history.replaceState(window.history.state, "", path);
  reportAccountId.value = account?.id;
});

watch(activePage, (page) => {
  if (page === "monthlyExpenses") reportsOpen.value = true;
});

watch([currentUser, activePage], ([user, page]) => {
  document.title = user && page !== "home" ? pageTitles[page] + " | Penny" : "Penny";
});

onMounted(async () => {
  window.addEventListener("popstate", syncPage);
  await loadCurrentUser();
  checkingSession.value = false;
});

onUnmounted(() => window.removeEventListener("popstate", syncPage));

async function signIn() {
  if (submitting.value) return;

  submitting.value = true;
  error.value = "";

  try {
    const response = await fetch("/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: email.value.trim(),
        password: password.value,
      }),
    });

    if (response.status === 401) {
      error.value = text.invalidCredentials;
      return;
    }

    if (!response.ok) {
      error.value = text.unavailable;
      return;
    }

    password.value = "";
    showPassword.value = false;
    if (!(await loadCurrentUser())) {
      error.value = text.sessionUnavailable;
    }
  } catch {
    error.value = text.unavailable;
  } finally {
    submitting.value = false;
  }
}

async function loadCurrentUser() {
  try {
    const response = await fetch("/auth/me", { cache: "no-store" });

    if (!response.ok) return false;

    syncPage();
    if (activePage.value === "monthlyExpenses") reportsOpen.value = true;
    currentUser.value = await response.json();
    return true;
  } catch {
    return false;
  }
}

function confirmSignOut() {
  const dialog = signOutDialog.value;
  if (!dialog) return;

  const confirmed = dialog.returnValue === "sign-out";
  dialog.returnValue = "";
  if (confirmed) void signOut();
}

async function signOut() {
  if (signingOut.value || savingProfile.value) return;

  signingOut.value = true;
  signOutError.value = "";

  try {
    const response = await fetch("/auth/logout", { method: "POST" });

    if (!response.ok && response.status !== 401) {
      signOutError.value = es.home.signOutUnavailable;
      return;
    }

    clearSession();
    window.history.replaceState(null, "", pagePaths.home);
    syncPage();
    email.value = "";
  } catch {
    signOutError.value = es.home.signOutUnavailable;
  } finally {
    signingOut.value = false;
  }
}
</script>

<template>
  <div v-if="currentUser" class="home-page">
    <aside class="home-sidebar">
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">P</span>
        <span>Penny</span>
      </div>
      <nav class="home-navigation" :aria-label="es.home.navigation">
        <a
          :href="pagePaths.home"
          :aria-current="activePage === 'home' ? 'page' : undefined"
          @click="navigate($event, 'home')"
        >
          <span class="home-nav-icon" aria-hidden="true"></span>
          {{ es.home.title }}
        </a>
        <a
          :href="movementPath(movementAccountId)"
          :aria-current="activePage === 'movements' ? 'page' : undefined"
          @click="navigate($event, 'movements')"
        >
          <span class="home-nav-icon home-nav-icon--movement" aria-hidden="true"></span>
          {{ es.home.movements }}
        </a>
        <a
          :href="pagePaths.categories"
          :aria-current="activePage === 'categories' ? 'page' : undefined"
          @click="navigate($event, 'categories')"
        >
          <span class="home-nav-icon home-nav-icon--category" aria-hidden="true"></span>
          {{ es.home.categories }}
        </a>
        <div class="reports-nav">
          <button
            type="button"
            :aria-expanded="reportsOpen"
            aria-controls="reports-nav-links"
            @click="reportsOpen = !reportsOpen"
          >
            <span class="home-nav-icon home-nav-icon--report" aria-hidden="true"></span>
            {{ es.home.reports }}
            <span class="reports-nav-chevron" aria-hidden="true"></span>
          </button>
          <div v-show="reportsOpen" id="reports-nav-links" class="reports-nav-links">
            <a
              :href="reportPath(reportAccountId ?? accounts?.[0]?.id, reportYear)"
              :aria-current="activePage === 'monthlyExpenses' ? 'page' : undefined"
              @click="navigate($event, 'monthlyExpenses')"
            >{{ es.report.title }}</a>
          </div>
        </div>
      </nav>
      <div class="home-profile">
        <a
          :href="pagePaths.profile"
          :aria-current="activePage === 'profile' ? 'page' : undefined"
          @click="navigate($event, 'profile')"
        >
          <span class="home-avatar" aria-hidden="true">{{
            currentUser.userName.slice(0, 1).toUpperCase()
          }}</span>
          <span>{{ es.home.profile }}</span>
        </a>
      </div>
    </aside>
    <div class="home-main">
      <header class="home-header">
        <h1 ref="pageHeading" tabindex="-1">
          {{ pageTitles[activePage] }}
        </h1>
        <button
          class="home-sign-out"
          type="button"
          :disabled="signingOut || savingProfile"
          @click="signOutDialog?.showModal()"
        >
          {{ signingOut ? es.home.signingOut : es.home.signOut }}
        </button>
        <p v-if="signOutError" class="auth-error home-sign-out-error" role="alert">
          {{ signOutError }}
        </p>
      </header>
      <main
        id="home-content"
        class="home-content"
        :class="{ 'home-content--report': activePage === 'monthlyExpenses' }"
        tabindex="-1"
      >
        <AccountOverview
          :key="currentUser.id"
          :show-overview="activePage === 'home'"
          :movement-revision="movementRevision"
          @accounts-changed="accounts = $event"
          @new-movement="movementDialog?.open($event)"
          @success="showSuccess"
          @session-expired="clearSession"
        >
          <template v-if="activePage === 'movements' || activePage === 'monthlyExpenses'" #default="{ accounts, loading, loadError, retry }">
            <p v-if="loading" role="status">{{ es.account.loading }}</p>
            <template v-else-if="loadError">
              <p class="auth-error" role="alert">{{ loadError }}</p>
              <button class="auth-submit" type="button" @click="retry">{{ es.account.retry }}</button>
            </template>
            <MovementHistory
              v-else-if="accounts?.length && activePage === 'movements'"
              :account-id="movementAccountId"
              :accounts="accounts"
              @account-selected="selectMovementAccount"
              @new-movement="movementDialog?.open($event)"
              @edit-movement="(accountId, movement) => movementDialog?.open(accountId, movement)"
              @session-expired="clearSession"
            />
            <MonthlyExpenseReport
              v-else-if="accounts?.length && reportAccountId !== undefined"
              :accounts="accounts"
              :account-id="reportAccountId"
              :year="reportYear"
              @filters-selected="selectReportFilters"
              @session-expired="clearSession"
            />
            <template v-else>
              <p>{{ activePage === 'movements' ? es.movement.accountRequired : es.report.accountRequired }}</p>
              <a class="home-back-link" :href="pagePaths.home" @click="navigate($event, 'home')">{{ es.home.title }}</a>
            </template>
          </template>
        </AccountOverview>
        <CategoryManager
          v-if="activePage === 'categories'"
          @success="showSuccess"
          @session-expired="clearSession"
        />
        <ProfileForm
          v-if="activePage === 'profile'"
          :key="currentUser.id"
          :profile="currentUser"
          :disabled="signingOut || savingProfile"
          :save="saveProfile"
        />
      </main>
    </div>
    <dialog
      ref="signOutDialog"
      class="app-dialog"
      aria-labelledby="sign-out-title"
      @close="confirmSignOut"
    >
      <h1 id="sign-out-title">{{ es.home.signOutConfirmation }}</h1>
      <form method="dialog" class="app-dialog-actions sign-out-dialog-actions">
        <button type="submit" value="cancel" autofocus>{{ es.home.cancelSignOut }}</button>
        <button class="auth-submit" type="submit" value="sign-out">{{ es.home.signOut }}</button>
      </form>
    </dialog>
    <SuccessToast ref="successToast" />
    <NewMovementDialog
      v-if="accounts?.length"
      ref="movementDialog"
      :accounts="accounts"
      @saved="movementSaved"
      @open-categories="openPage('categories')"
      @session-expired="clearSession"
    />
  </div>

  <main v-else class="auth-page">
    <section class="auth-card" aria-labelledby="auth-title">
      <div class="brand auth-brand">
        <span class="brand-mark" aria-hidden="true">P</span>
        <span>Penny</span>
      </div>

      <h1 v-if="checkingSession" id="auth-title" role="status">
        {{ text.checkingSession }}
      </h1>

      <template v-else>
        <div class="auth-heading">
          <h1 id="auth-title">{{ text.title }}</h1>
          <p>{{ text.description }}</p>
        </div>

        <form class="auth-form" @submit.prevent="signIn">
          <div class="auth-field">
            <label for="email">{{ text.emailLabel }}</label>
            <input
              id="email"
              v-model="email"
              type="email"
              autocomplete="username"
              required
            />
          </div>

          <div class="auth-field">
            <label for="password">{{ text.passwordLabel }}</label>
            <div class="password-field">
              <input
                id="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                required
              />
              <button
                class="password-toggle"
                type="button"
                aria-controls="password"
                :aria-label="
                  showPassword ? text.hidePassword : text.showPassword
                "
                :title="showPassword ? text.hidePassword : text.showPassword"
                @click="showPassword = !showPassword"
              >
                <svg
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.75"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  aria-hidden="true"
                >
                  <template v-if="showPassword">
                    <path d="M3 3l18 18M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                    <path
                      d="M9.9 5.2c.7-.1 1.4-.2 2.1-.2 6 0 10 7 10 7a17 17 0 0 1-3 3.6M6.4 6.4A19 19 0 0 0 2 12s4 7 10 7a9 9 0 0 0 4.2-1.1"
                    />
                  </template>
                  <template v-else>
                    <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7Z" />
                    <circle cx="12" cy="12" r="3" />
                  </template>
                </svg>
              </button>
            </div>
          </div>

          <p v-if="error" class="auth-error" role="alert">{{ error }}</p>

          <button class="auth-submit" type="submit" :disabled="submitting">
            {{ submitting ? text.submitting : text.submit }}
          </button>
        </form>
      </template>
    </section>
  </main>
</template>
