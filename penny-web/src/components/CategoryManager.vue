<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from "vue";
import { es } from "../locales/es";

type Subcategory = { id: number; name: string };
type Category = { id: number; name: string; subcategories: Subcategory[] };
type DeletionTarget = { category: Category; subcategory?: Subcategory };

const emit = defineEmits<{ "session-expired": [] }>();
const text = es.category;
const categories = ref<Category[]>([]);
const categoryName = ref("");
const subcategoryName = ref("");
const parentId = ref<number>();
const categoryFormOpen = ref(false);
const subcategoryFormOpen = ref(false);
const categoryInput = ref<HTMLInputElement>();
const subcategoryInput = ref<HTMLInputElement | null>(null);
const selectedCategory = computed(() => categories.value.find((category) => category.id === parentId.value));
const loading = ref(true);
const saving = ref(false);
const error = ref("");
const duplicateError = ref("");
const deleteError = ref("");
const deleting = ref(false);
const deletionTarget = ref<DeletionTarget>();
const deleteDialog = ref<HTMLDialogElement>();
const deleteErrorButton = ref<HTMLButtonElement>();
const listHeading = ref<HTMLHeadingElement>();
const status = ref("");
const byName = new Intl.Collator("es-AR");

onMounted(loadCategories);

async function loadCategories() {
  loading.value = true;
  error.value = "";
  duplicateError.value = "";
  try {
    const response = await fetch("/category", { cache: "no-store" });
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (!response.ok) throw new Error();

    categories.value = await response.json();
    if (!selectedCategory.value) parentId.value = categories.value[0]?.id;
  } catch {
    error.value = text.loadError;
  } finally {
    loading.value = false;
  }
}

async function createCategory(parent?: Category) {
  const input = parent ? subcategoryName : categoryName;
  const name = input.value.trim();
  if (!name || loading.value || saving.value || deleting.value || error.value) return;

  saving.value = true;
  duplicateError.value = "";
  status.value = "";
  try {
    const path = parent ? "/category/" + parent.id + "/subcategory" : "/category";
    const response = await fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (response.status === 409) {
      duplicateError.value = parent ? text.duplicateSubcategory : text.duplicateCategory;
      return;
    }
    if (response.status !== 201) throw new Error();

    if (parent) {
      const subcategory: Subcategory = await response.json();
      parent.subcategories.push(subcategory);
      parent.subcategories.sort((first, second) => byName.compare(first.name, second.name));
      status.value = text.subcategoryCreated;
    } else {
      const category: Category = await response.json();
      categories.value.push(category);
      categories.value.sort((first, second) => byName.compare(first.name, second.name));
      parentId.value = category.id;
      subcategoryFormOpen.value = false;
      subcategoryName.value = "";
      status.value = text.categoryCreated;
    }
    input.value = "";
  } catch {
    error.value = text.saveError;
  } finally {
    saving.value = false;
  }
}

function createSubcategory() {
  if (selectedCategory.value) return createCategory(selectedCategory.value);
}

function selectCategory(id: number) {
  parentId.value = parentId.value === id ? undefined : id;
  subcategoryFormOpen.value = false;
  subcategoryName.value = "";
  duplicateError.value = "";
}

async function confirmDeletion(category: Category, subcategory?: Subcategory) {
  if (saving.value || deleting.value || deletionTarget.value) return;

  deleteError.value = "";
  status.value = "";
  deletionTarget.value = { category, subcategory };
  await nextTick();
  deleteDialog.value?.showModal();
}

async function finishDeletion() {
  const target = deletionTarget.value;
  if (!target || deleting.value || deleteError.value) return;

  const { category, subcategory } = target;
  const categoryPath = "/category/" + category.id;
  const path = subcategory ? categoryPath + "/subcategory/" + subcategory.id : categoryPath;
  deleting.value = true;

  try {
    const response = await fetch(path, { method: "DELETE" });
    if (response.status === 401) {
      emit("session-expired");
      return;
    }
    if (response.status === 409) {
      const { code } = await response.json();
      deleteError.value = text.deleteConflicts[code as keyof typeof text.deleteConflicts] ?? text.deleteError;
      return;
    }
    if (response.status !== 204 && response.status !== 404) throw new Error();

    if (subcategory) {
      category.subcategories = category.subcategories.filter((item) => item.id !== subcategory.id);
    } else {
      if (parentId.value === category.id) selectCategory(category.id);
      categories.value = categories.value.filter((item) => item.id !== category.id);
    }
    status.value = response.status === 404 ? text.alreadyRemoved
      : subcategory ? text.subcategoryDeleted : text.categoryDeleted;
    deleteDialog.value?.close();
    await nextTick();
    listHeading.value?.focus();
  } catch {
    deleteError.value = text.deleteError;
  } finally {
    deleting.value = false;
    if (deleteError.value) {
      await nextTick();
      deleteErrorButton.value?.focus();
    }
  }
}

function resetDeletion() {
  deletionTarget.value = undefined;
  deleteError.value = "";
}

async function toggleForm(form: "category" | "subcategory") {
  duplicateError.value = "";
  const open = form === "category" ? categoryFormOpen : subcategoryFormOpen;
  open.value = !open.value;
  if (open.value) {
    await nextTick();
    (form === "category" ? categoryInput : subcategoryInput).value?.focus();
  }
}
</script>

<template>
  <div class="category-manager">
    <p class="category-status" role="status">{{ loading ? text.loading : deleting ? text.deleting : saving ? text.saving : status }}</p>
    <div v-if="error">
      <p class="auth-error" role="alert">{{ error }}</p>
      <button class="auth-submit" type="button" @click="loadCategories">{{ text.retry }}</button>
    </div>

    <template v-else-if="!loading">
      <div class="category-toolbar">
        <p>{{ text.description }}</p>
        <button class="auth-submit" type="button" :aria-expanded="categoryFormOpen" aria-controls="category-form" :disabled="saving || deleting" @click="toggleForm('category')">
          <span aria-hidden="true">+</span> {{ text.newCategory }}
        </button>
      </div>
      <div class="category-layout">
        <section class="category-list" aria-labelledby="category-list-title">
          <header class="category-list-heading">
            <h2 id="category-list-title" ref="listHeading" tabindex="-1">{{ text.list }}</h2>
            <span>{{ text.count(categories.length) }}</span>
          </header>
          <p v-if="!categories.length" class="category-empty">{{ text.empty }}</p>
          <article v-for="category in categories" :key="category.id">
            <div class="category-row-actions">
            <button
              class="category-row"
              type="button"
              :aria-expanded="parentId === category.id"
              :aria-controls="'category-' + category.id"
              :disabled="saving || deleting"
              @click="selectCategory(category.id)"
            >
              <span class="category-marker" aria-hidden="true"></span>
              <span><strong>{{ category.name }}</strong><small>{{ text.subcategoryCount(category.subcategories.length) }}</small></span>
              <span class="category-disclosure" aria-hidden="true">{{ parentId === category.id ? "−" : "+" }}</span>
            </button>
              <button class="category-delete" type="button" :aria-label="text.deleteNamed(category.name)" :disabled="saving || deleting" @click="confirmDeletion(category)">
                {{ text.delete }}
              </button>
            </div>
            <div v-show="parentId === category.id" :id="'category-' + category.id" class="category-children">
              <ul v-if="category.subcategories.length">
                <li v-for="subcategory in category.subcategories" :key="subcategory.id">
                  <span>{{ subcategory.name }}</span>
                  <button class="category-delete" type="button" :aria-label="text.deleteNamed(subcategory.name)" :disabled="saving || deleting" @click="confirmDeletion(category, subcategory)">
                    {{ text.delete }}
                  </button>
                </li>
              </ul>
              <button
                class="category-add"
                type="button"
                :aria-expanded="subcategoryFormOpen && parentId === category.id"
                :disabled="saving || deleting"
                @click="toggleForm('subcategory')"
              >
                <span aria-hidden="true">+</span> {{ text.newSubcategory }}
              </button>
              <form
                v-if="subcategoryFormOpen && parentId === category.id"
                class="auth-form category-inline-form"
                :action="'/category/' + category.id + '/subcategory'"
                method="post"
                @submit.prevent="createSubcategory"
              >
                <div class="auth-field">
                  <label for="subcategory-name">{{ text.subcategoryName }}</label>
                  <input
                    id="subcategory-name"
                    :ref="(element) => subcategoryInput = element as HTMLInputElement | null"
                    v-model="subcategoryName"
                    name="name"
                    pattern=".*\S.*"
                    required
                    :readonly="saving"
                  />
                </div>
                <p v-if="duplicateError === text.duplicateSubcategory" class="auth-error" role="alert">{{ duplicateError }}</p>
                <button class="auth-submit" type="submit" :disabled="saving || deleting">{{ text.createSubcategory }}</button>
              </form>
            </div>
          </article>
        </section>
        <section v-show="categoryFormOpen" id="category-form" class="account-panel category-create">
          <h2 id="new-category">{{ text.newCategory }}</h2>
          <form class="auth-form" aria-labelledby="new-category" action="/category" method="post" @submit.prevent="createCategory()">
            <div class="auth-field">
              <label for="category-name">{{ text.categoryName }}</label>
              <input id="category-name" ref="categoryInput" v-model="categoryName" name="name" pattern=".*\S.*" required :readonly="saving" />
            </div>
            <p v-if="duplicateError === text.duplicateCategory" class="auth-error" role="alert">{{ duplicateError }}</p>
            <button class="auth-submit" type="submit" :disabled="saving || deleting">{{ text.createCategory }}</button>
          </form>
        </section>
        <section v-if="selectedCategory && !categoryFormOpen" class="account-panel category-preview">
          <span class="category-marker" aria-hidden="true"></span>
          <div>
            <h2>{{ selectedCategory.name }}</h2>
            <p>{{ text.subcategoryCount(selectedCategory.subcategories.length) }}</p>
          </div>
        </section>
      </div>
    </template>
    <dialog
      ref="deleteDialog"
      class="category-delete-dialog"
      aria-labelledby="delete-category-title"
      aria-describedby="delete-category-description"
      @cancel="deleting && $event.preventDefault()"
      @close="resetDeletion"
    >
      <h1 id="delete-category-title">
        {{ deleteError ? text.deleteFailed : deletionTarget?.subcategory ? text.deleteSubcategory : text.deleteCategory }}
      </h1>
      <p id="delete-category-description">
        {{ deleteError || text.deleteConfirmation(deletionTarget?.subcategory?.name ?? deletionTarget?.category.name ?? "") }}
      </p>
      <form v-if="deleteError" method="dialog" class="category-dialog-actions">
        <button ref="deleteErrorButton" class="auth-submit" type="submit" aria-describedby="delete-category-description">
          {{ text.close }}
        </button>
      </form>
      <form v-else class="category-dialog-actions" @submit.prevent="finishDeletion">
        <button type="button" autofocus :disabled="deleting" @click="deleteDialog?.close()">{{ text.cancel }}</button>
        <button class="category-delete-confirm" type="submit" :disabled="deleting">
          {{ deleting ? text.deleting : text.delete }}
        </button>
      </form>
    </dialog>
  </div>
</template>
