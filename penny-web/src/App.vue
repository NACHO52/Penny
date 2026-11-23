<script setup lang="ts">
import { onMounted, ref } from "vue";
import { es } from "./locales/es";

type CurrentUser = {
  id: string;
  email: string;
};

const text = es.login;
const email = ref("");
const password = ref("");
const showPassword = ref(false);
const submitting = ref(false);
const error = ref("");
const currentUser = ref<CurrentUser | null>(null);
const checkingSession = ref(true);
const signingOut = ref(false);
const signOutError = ref("");

onMounted(async () => {
  await loadCurrentUser();
  checkingSession.value = false;
});

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

    currentUser.value = await response.json();
    return true;
  } catch {
    return false;
  }
}

async function signOut() {
  if (signingOut.value) return;

  signingOut.value = true;
  signOutError.value = "";

  try {
    const response = await fetch("/auth/logout", { method: "POST" });

    if (!response.ok && response.status !== 401) {
      signOutError.value = es.home.signOutUnavailable;
      return;
    }

    currentUser.value = null;
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
        <a href="#home-content" aria-current="page">
          <span class="home-nav-icon" aria-hidden="true"></span>
          {{ es.home.title }}
        </a>
      </nav>
    </aside>
    <div class="home-main">
      <header class="home-header">
        <h1>{{ es.home.title }}</h1>
        <button class="home-sign-out" type="button" :disabled="signingOut" @click="signOut">
          {{ signingOut ? es.home.signingOut : es.home.signOut }}
        </button>
      </header>
      <main id="home-content" class="home-content" tabindex="-1">
        <p class="session-email">{{ currentUser.email }}</p>
        <p v-if="signOutError" class="auth-error" role="alert">{{ signOutError }}</p>
      </main>
    </div>
  </div>

  <main v-else class="auth-page">
    <section class="auth-card" aria-labelledby="auth-title">
      <div class="brand auth-brand">
        <span class="brand-mark" aria-hidden="true">P</span>
        <span>Penny</span>
      </div>

      <h1 v-if="checkingSession" id="auth-title" role="status">{{ text.checkingSession }}</h1>

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
                :aria-label="showPassword ? text.hidePassword : text.showPassword"
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
                    <path d="M9.9 5.2c.7-.1 1.4-.2 2.1-.2 6 0 10 7 10 7a17 17 0 0 1-3 3.6M6.4 6.4A19 19 0 0 0 2 12s4 7 10 7a9 9 0 0 0 4.2-1.1" />
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
