import assert from "node:assert/strict";
import test from "node:test";
import { isOverdue, selectVisibleTasks } from "../../src/TodoManager/wwwroot/task-list.mjs";

const task = (overrides = {}) => ({
  id: 1,
  title: "Plan the week",
  notes: "",
  priority: "Medium",
  dueDate: null,
  isCompleted: false,
  ...overrides,
});

test("overdue status uses an explicit local date and excludes completed or undated tasks", () => {
  assert.equal(isOverdue(task({ dueDate: "2028-01-30" }), "2028-01-31"), true);
  assert.equal(isOverdue(task({ dueDate: "2028-01-31" }), "2028-01-31"), false);
  assert.equal(isOverdue(task({ dueDate: "2028-01-30", isCompleted: true }), "2028-01-31"), false);
  assert.equal(isOverdue(task(), "2028-01-31"), false);
});

test("filters by completion and case-insensitive title or notes search", () => {
  const tasks = [
    task({ id: 1, notes: "Quarterly Review" }),
    task({ id: 2, title: "Buy groceries", isCompleted: true }),
  ];

  assert.deepEqual(
    selectVisibleTasks(tasks, { filter: "completed", query: "  QUARTERLY ", sort: "recent" }),
    [],
  );
  assert.deepEqual(
    selectVisibleTasks(tasks, { filter: "all", query: "  QUARTERLY ", sort: "recent" }),
    [tasks[0]],
  );
  assert.deepEqual(
    selectVisibleTasks(tasks, { filter: "completed", query: "", sort: "recent" }),
    [tasks[1]],
  );
});

test("sorts by due date and priority with descending IDs for ties", () => {
  const tasks = [
    task({ id: 1, title: "No due date", priority: "High" }),
    task({ id: 2, title: "Later", priority: "Low", dueDate: "2028-02-02" }),
    task({ id: 3, title: "Earlier", priority: "Low", dueDate: "2028-02-01" }),
    task({ id: 4, title: "Earlier tie", priority: "High", dueDate: "2028-02-01" }),
  ];

  assert.deepEqual(
    selectVisibleTasks(tasks, { filter: "all", query: "", sort: "due" }).map(({ id }) => id),
    [4, 3, 2, 1],
  );
  assert.deepEqual(
    selectVisibleTasks(tasks, { filter: "all", query: "", sort: "priority" }).map(({ id }) => id),
    [4, 1, 3, 2],
  );
  assert.deepEqual(
    selectVisibleTasks(tasks, { filter: "all", query: "", sort: "recent" }).map(({ id }) => id),
    [4, 3, 2, 1],
  );
});
