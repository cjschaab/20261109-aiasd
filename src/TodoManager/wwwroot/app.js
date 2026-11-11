"use strict";

const byId = (id) => document.getElementById(id);
let tasks = [];
let filter = "all";
let editingId = null;
let busy = false;
let loaded = false;

function showError(error) {
  byId("error").textContent = error.message;
  byId("error").hidden = false;
}

async function request(path = "", options = {}) {
  const response = await fetch(`/api/tasks${path}`, {
    ...options,
    headers: { "Content-Type": "application/json" },
  });
  if (!response.ok) {
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

function overdue(task) {
  return !task.isCompleted && task.dueDate && task.dueDate < today();
}

function element(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function render() {
  const completed = tasks.filter((task) => task.isCompleted).length;
  byId("active-count").textContent = tasks.length - completed;
  byId("completed-count").textContent = completed;
  byId("overdue-count").textContent = tasks.filter(overdue).length;
  const query = byId("search").value.trim().toLocaleLowerCase();
  const visible = tasks.filter((task) =>
    (filter === "all" || (filter === "completed" ? task.isCompleted : !task.isCompleted)) &&
    `${task.title} ${task.notes}`.toLocaleLowerCase().includes(query));
  const ranks = { High: 0, Medium: 1, Low: 2 };
  visible.sort((a, b) => {
    if (byId("sort").value === "due")
      return (a.dueDate || "9999-12-31").localeCompare(b.dueDate || "9999-12-31") || b.id - a.id;
    if (byId("sort").value === "priority") return ranks[a.priority] - ranks[b.priority] || b.id - a.id;
    return b.id - a.id;
  });
  byId("visible-count").textContent = visible.length;
  byId("tasks").replaceChildren(...visible.map(taskCard));
  byId("empty").hidden = !loaded || visible.length > 0;
  byId("empty-title").textContent = tasks.length ? "Nothing here. A little breathing room." : "A fresh start.";
  byId("empty-message").textContent = tasks.length ? "Try another filter or search, or add a new task." : "Add your first task and make a little progress today.";
  document.querySelectorAll("[data-filter]").forEach((button) =>
    button.setAttribute("aria-pressed", String(button.dataset.filter === filter)));
  document.querySelectorAll("#tasks button, #tasks input, #refresh, #save, #cancel").forEach((control) => {
    control.disabled = busy;
  });
}

function taskCard(task) {
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
    const due = element("time", `due${overdue(task) ? " overdue" : ""}`, `${overdue(task) ? "Overdue / " : "Due "}${formatted}`);
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

byId("task-form").addEventListener("submit", (event) => {
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
  mutate(async () => {
    const saved = await request(editingId === null ? "" : `/${editingId}`, {
      method: editingId === null ? "POST" : "PUT", body: JSON.stringify(input),
    });
    if (editingId === null) tasks.unshift(saved);
    else tasks = tasks.map((task) => task.id === editingId ? saved : task);
    announce(editingId === null ? "Task added." : "Task updated.");
    resetForm();
    byId("title").focus();
  });
});
byId("cancel").addEventListener("click", resetForm);
byId("refresh").addEventListener("click", load);
byId("search").addEventListener("input", render);
byId("sort").addEventListener("change", render);
document.querySelectorAll("[data-filter]").forEach((button) =>
  button.addEventListener("click", () => { filter = button.dataset.filter; render(); }));
load();
