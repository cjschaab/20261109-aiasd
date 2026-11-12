"use strict";

import { isOverdue, selectVisibleTasks } from "./task-list.mjs";

const byId = (id) => document.getElementById(id);
let tasks = [];
let filter = "all";
let editingId = null;
let busy = false;
let loaded = false;
let loginEnabled = true;
let csrfToken = null;
let authBusy = false;

function showError(error) {
  byId("error").textContent = error.message;
  byId("error").hidden = false;
}

async function request(path = "", options = {}) {
  return apiRequest(`/api/tasks${path}`, options, true);
}

async function apiRequest(url, options = {}, taskRequest = false) {
  const response = await fetch(url, {
    ...options,
    cache: "no-store",
    headers: {
      "Content-Type": "application/json",
      ...(csrfToken ? { "X-CSRF-TOKEN": csrfToken } : {}),
      ...options.headers,
    },
  });
  if (!response.ok) {
    if ((taskRequest || url === "/api/auth/logout") && response.status === 401 && loginEnabled) {
      showSignedOut();
      throw new Error("Your session has ended. Please sign in again.");
    }
    const body = await response.text();
    let problem;
    try { problem = JSON.parse(body); } catch { /* Non-JSON errors use the status below. */ }
    const details = problem?.errors ? Object.values(problem.errors).flat().join("\n") : problem?.detail;
    throw new Error(details || `Request failed (${response.status}). Refresh and try again.`);
  }
  return response.status === 204 ? null : response.json();
}

function today() {
  const date = new Date();
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

function element(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function render() {
  byId("sign-out").disabled = busy || authBusy;
  const currentDate = today();
  const completed = tasks.filter((task) => task.isCompleted).length;
  byId("active-count").textContent = tasks.length - completed;
  byId("completed-count").textContent = completed;
  byId("overdue-count").textContent = tasks.filter((task) => isOverdue(task, currentDate)).length;
  const visible = selectVisibleTasks(tasks, {
    filter,
    query: byId("search").value,
    sort: byId("sort").value,
  });
  byId("visible-count").textContent = visible.length;
  byId("tasks").replaceChildren(...visible.map((task) => taskCard(task, currentDate)));
  byId("empty").hidden = !loaded || visible.length > 0;
  byId("empty-title").textContent = tasks.length ? "Nothing here. A little breathing room." : "A fresh start.";
  byId("empty-message").textContent = tasks.length ? "Try another filter or search, or add a new task." : "Add your first task and make a little progress today.";
  document.querySelectorAll("[data-filter]").forEach((button) =>
    button.setAttribute("aria-pressed", String(button.dataset.filter === filter)));
  document.querySelectorAll("#tasks button, #tasks input, #refresh, #task-form input, #task-form textarea, #task-form select, #task-form button").forEach((control) => {
    control.disabled = busy;
  });
}

function taskCard(task, currentDate) {
  const card = element("li", `task-card${task.isCompleted ? " completed" : ""}`);
  const checkbox = element("input", "task-check");
  checkbox.type = "checkbox";
  checkbox.checked = task.isCompleted;
  checkbox.setAttribute("aria-label", `Mark "${task.title}" ${task.isCompleted ? "incomplete" : "complete"}`);
  checkbox.addEventListener("change", () => mutate(async () => {
    const updated = await request(`/${task.id}`, {
      method: "PUT", body: JSON.stringify(payload(task, { isCompleted: !task.isCompleted })),
    });
    tasks = tasks.map((item) => item.id === task.id ? updated : item);
    if (updated.isCompleted && updated.recurrence !== "None")
      tasks = await request();
    announce(updated.isCompleted ? "Task completed." : "Task reopened.");
  }));
  const content = element("div", "task-content");
  content.append(element("h3", "task-title", task.title));
  if (task.notes) content.append(element("p", "task-notes", task.notes));
  const meta = element("div", "task-meta");
  meta.append(element("span", `badge ${task.priority.toLowerCase()}`, `${task.priority} priority`));
  if (task.recurrence !== "None")
    meta.append(element("span", "badge", `Repeats ${task.recurrence.toLowerCase()}`));
  if (task.dueDate) {
    const date = new Date(`${task.dueDate}T00:00:00`);
    const formatted = date.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" });
    const taskIsOverdue = isOverdue(task, currentDate);
    const due = element("time", `due${taskIsOverdue ? " overdue" : ""}`, `${taskIsOverdue ? "Overdue / " : "Due "}${formatted}`);
    due.dateTime = task.dueDate;
    meta.append(due);
  }
  content.append(meta);
  const actions = element("div", "task-actions");
  const edit = element("button", "", "Edit");
  edit.type = "button";
  edit.setAttribute("aria-label", `Edit "${task.title}"`);
  edit.addEventListener("click", () => editTask(task));
  const remove = element("button", "delete", "Delete");
  remove.type = "button";
  remove.setAttribute("aria-label", `Delete "${task.title}"`);
  remove.addEventListener("click", () => {
    if (!window.confirm(`Delete "${task.title}"? This cannot be undone.`)) return;
    mutate(async () => {
      await request(`/${task.id}`, { method: "DELETE" });
      tasks = tasks.filter((item) => item.id !== task.id);
      if (editingId === task.id) resetForm();
      announce("Task deleted.");
    });
  });
  actions.append(edit, remove);
  card.append(checkbox, content, actions);
  return card;
}

function payload(task, changes = {}) {
  return { title: task.title, notes: task.notes, priority: task.priority,
    dueDate: task.dueDate, isCompleted: task.isCompleted, recurrence: task.recurrence, ...changes };
}

function announce(message) {
  byId("announcement").textContent = message;
}

async function mutate(action) {
  if (busy) return;
  busy = true;
  byId("error").hidden = true;
  render();
  try { await action(); } catch (error) { showError(error); }
  finally { busy = false; render(); }
}

async function load() {
  await mutate(async () => {
    byId("loading").hidden = false;
    try {
      tasks = await request();
      loaded = true;
      announce("Tasks refreshed.");
    } finally { byId("loading").hidden = true; }
  });
}

function editTask(task) {
  editingId = task.id;
  byId("title").value = task.title;
  byId("notes").value = task.notes;
  byId("priority").value = task.priority;
  byId("due-date").value = task.dueDate || "";
  byId("recurrence").value = task.recurrence;
  byId("form-heading").textContent = "Edit task";
  byId("form-eyebrow").textContent = "A CHANGE OF PLAN";
  byId("save").textContent = "Save changes";
  byId("cancel").hidden = false;
  byId("title").focus();
}

function resetForm() {
  editingId = null;
  byId("task-form").reset();
  byId("form-heading").textContent = "Add a task";
  byId("form-eyebrow").textContent = "START SOMETHING";
  byId("save").textContent = "+ Add task";
  byId("cancel").hidden = true;
}

byId("task-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const title = byId("title").value.trim();
  if (!title) {
    showError(new Error("Please give your task a name."));
    byId("title").focus();
    return;
  }
  const existing = tasks.find((task) => task.id === editingId);
  const input = {
    title, notes: byId("notes").value.trim(), priority: byId("priority").value,
    dueDate: byId("due-date").value || null, isCompleted: existing?.isCompleted ?? false,
    recurrence: byId("recurrence").value,
  };
  await mutate(async () => {
    const saved = await request(editingId === null ? "" : `/${editingId}`, {
      method: editingId === null ? "POST" : "PUT", body: JSON.stringify(input),
    });
    if (editingId === null) tasks.unshift(saved);
    else tasks = tasks.map((task) => task.id === editingId ? saved : task);
    announce(editingId === null ? "Task added." : "Task updated.");
    resetForm();
  });
  if (byId("error").hidden) byId("title").focus();
});
byId("cancel").addEventListener("click", resetForm);
byId("refresh").addEventListener("click", load);
byId("search").addEventListener("input", render);
byId("sort").addEventListener("change", render);
document.querySelectorAll("[data-filter]").forEach((button) =>
  button.addEventListener("click", () => { filter = button.dataset.filter; render(); }));

function showSignedOut() {
  tasks = [];
  loaded = false;
  csrfToken = null;
  filter = "all";
  byId("search").value = "";
  resetForm();
  render();
  byId("workspace-content").hidden = true;
  byId("account-controls").hidden = true;
  byId("current-user").textContent = "";
  byId("login-panel").hidden = false;
  byId("password").value = "";
  byId("username").focus();
}

async function readSession() {
  const session = await apiRequest("/api/auth/session");
  loginEnabled = session.loginEnabled;
  csrfToken = session.csrfToken;
  byId("login-panel").hidden = !loginEnabled || session.isAuthenticated;
  byId("workspace-content").hidden = loginEnabled && !session.isAuthenticated;
  byId("account-controls").hidden = !loginEnabled || !session.isAuthenticated;
  byId("current-user").textContent = session.userName || "";
  return session;
}

async function authAction(action) {
  if (authBusy || busy) return;
  authBusy = true;
  byId("error").hidden = true;
  document.querySelectorAll("#login-form input, #login-form button, #auth-retry, #sign-out")
    .forEach(control => { control.disabled = true; });
  try { await action(); } catch (error) { showError(error); }
  finally {
    authBusy = false;
    document.querySelectorAll("#login-form input, #login-form button, #auth-retry, #sign-out")
      .forEach(control => { control.disabled = false; });
    byId("password").value = "";
  }
}

async function initializeAuth() {
  await authAction(async () => {
    byId("auth-loading").hidden = false;
    byId("auth-retry").hidden = true;
    try {
      const session = await readSession();
      if (!session.loginEnabled || session.isAuthenticated) await load();
      else byId("username").focus();
    } catch (error) {
      byId("workspace-content").hidden = true;
      byId("auth-retry").hidden = false;
      throw error;
    } finally { byId("auth-loading").hidden = true; }
  });
}

byId("login-form").addEventListener("submit", event => {
  event.preventDefault();
  const credentials = { userName: byId("username").value.trim(), password: byId("password").value };
  authAction(async () => {
    await readSession();
    await apiRequest("/api/auth/login", { method: "POST", body: JSON.stringify(credentials) });
    await readSession();
    await load();
    byId("title").focus();
  });
});
byId("sign-out").addEventListener("click", () => authAction(async () => {
  await apiRequest("/api/auth/logout", { method: "POST" });
  showSignedOut();
  announce("Signed out.");
}));
byId("auth-retry").addEventListener("click", initializeAuth);
initializeAuth();
