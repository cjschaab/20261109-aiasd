import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page, request }) => {
  const response = await request.get("/api/tasks");
  expect(response.ok()).toBeTruthy();
  const tasks: { id: number }[] = await response.json();
  for (const task of tasks) {
    const deleted = await request.delete(`/api/tasks/${task.id}`);
    expect(deleted.ok()).toBeTruthy();
  }

  await page.goto("/");
  await expect(page.locator("#loading")).toBeHidden();
});

test("creates and edits a task", async ({ page }) => {
  await page.getByLabel("What needs doing?").fill("Plan the week");
  await page.getByLabel("Notes (optional)").fill("Choose the top priorities");
  await page.getByLabel("Priority", { exact: true }).selectOption("High");
  await page.getByLabel("Due date", { exact: true }).fill("2028-02-01");
  await page.getByRole("button", { name: "+ Add task" }).click();

  const card = page.locator(".task-card");
  await expect(card).toHaveCount(1);
  await expect(card).toContainText("Plan the week");
  await expect(card).toContainText("Choose the top priorities");
  await expect(card.locator("time")).toHaveAttribute("datetime", "2028-02-01");

  await card.getByRole("button", { name: 'Edit "Plan the week"' }).click();
  await page.getByLabel("What needs doing?").fill("Plan next week");
  await page.getByRole("button", { name: "Save changes" }).click();

  await expect(page.locator(".task-card")).toContainText("Plan next week");
  await expect(page.locator(".task-card")).not.toContainText("Plan the week");
});

test("filters and searches tasks, then sorts by priority", async ({ page }) => {
  const entries = [
    { title: "Low priority", priority: "Low" },
    { title: "High priority", priority: "High" },
    { title: "Medium priority", priority: "Medium" },
  ];

  for (const [index, entry] of entries.entries()) {
    await page.getByLabel("What needs doing?").fill(entry.title);
    await page.getByLabel("Priority", { exact: true }).selectOption(entry.priority);
    await page.getByRole("button", { name: "+ Add task" }).click();
    await expect(page.locator(".task-card")).toHaveCount(index + 1);
  }

  await page.getByRole("checkbox", { name: 'Mark "Medium priority" complete' }).click();
  await page.getByRole("button", { name: "Completed" }).click();
  await expect(page.locator(".task-card")).toHaveCount(1);
  await expect(page.locator(".task-card")).toContainText("Medium priority");

  await page.getByRole("button", { name: "To do" }).click();
  await page.getByLabel("Search titles and notes").fill("high");
  await expect(page.locator(".task-card")).toHaveCount(1);
  await expect(page.locator(".task-card")).toContainText("High priority");

  await page.getByLabel("Search titles and notes").clear();
  await page.getByRole("button", { name: "All tasks" }).click();
  await page.getByLabel("Sort by").selectOption("priority");
  await expect(page.locator(".task-title")).toHaveText([
    "High priority",
    "Medium priority",
    "Low priority",
  ]);
});

test("completes a recurring task and confirms deleting its successor", async ({ page }) => {
  await page.getByLabel("What needs doing?").fill("Weekly review");
  await page.getByLabel("Repeat").selectOption("Weekly");
  await page.getByRole("button", { name: "+ Add task" }).click();

  await page.getByRole("checkbox", { name: 'Mark "Weekly review" complete' }).click();
  await expect(page.locator(".task-card")).toHaveCount(2);
  await expect(page.locator(".task-card.completed")).toHaveCount(1);
  const successor = page.locator(".task-card:not(.completed)");
  await expect(successor).toContainText("Repeats weekly");
  await expect(successor.locator("time")).toHaveAttribute("datetime", /^\d{4}-\d{2}-\d{2}$/);

  page.once("dialog", (dialog) => dialog.accept());
  await successor.getByRole("button", { name: 'Delete "Weekly review"' }).click();
  await expect(page.locator(".task-card")).toHaveCount(1);
  await expect(page.locator(".task-card.completed")).toHaveCount(1);
});

test("shows an API error and allows the task to be retried", async ({ page }) => {
  await page.route("**/api/tasks", (route) =>
    route.fulfill({
      status: 503,
      contentType: "application/problem+json",
      body: JSON.stringify({ detail: "Unable to save right now." }),
    }));

  await page.getByLabel("What needs doing?").fill("Retry this task");
  await page.getByRole("button", { name: "+ Add task" }).click();
  await expect(page.getByRole("alert")).toContainText("Unable to save right now.");

  await page.unroute("**/api/tasks");
  await page.getByRole("button", { name: "+ Add task" }).click();
  await expect(page.locator(".task-card")).toContainText("Retry this task");
});

for (const outcome of ["success", "failure"] as const) {
  test(`locks the form during a pending save and unlocks after ${outcome}`, async ({ page }) => {
    let releaseSave = () => {};
    const pendingSave = new Promise<void>((resolve) => { releaseSave = resolve; });
    await page.route("**/api/tasks", async (route) => {
      await pendingSave;
      if (outcome === "success") {
        await route.continue();
      } else {
        await route.fulfill({
          status: 503,
          contentType: "application/problem+json",
          body: JSON.stringify({ detail: "Unable to save right now." }),
        });
      }
    });

    await page.getByLabel("What needs doing?").fill("Pending task");
    await page.getByLabel("Notes (optional)").fill("Keep these notes");
    await page.getByLabel("Priority", { exact: true }).selectOption("High");
    await page.getByLabel("Due date", { exact: true }).fill("2028-02-01");
    await page.getByLabel("Repeat").selectOption("Weekly");
    const fields = page.locator("#task-form input, #task-form textarea, #task-form select");

    try {
      await Promise.all([
        page.waitForRequest((request) =>
          request.method() === "POST" && request.url().endsWith("/api/tasks")),
        page.getByRole("button", { name: "+ Add task" }).click(),
      ]);
      for (const field of await fields.all())
        await expect(field).toBeDisabled();
      await expect(page.getByRole("button", { name: "+ Add task" })).toBeDisabled();
    } finally {
      releaseSave();
      await page.unrouteAll({ behavior: "wait" });
    }

    await expect(page.getByRole("button", { name: "+ Add task" })).toBeEnabled();
    for (const field of await fields.all())
      await expect(field).toBeEnabled();

    if (outcome === "success") {
      await expect(page.locator(".task-card")).toContainText("Pending task");
      await expect(page.getByLabel("What needs doing?")).toHaveValue("");
      await expect(page.getByLabel("What needs doing?")).toBeFocused();
    } else {
      await expect(page.getByRole("alert")).toContainText("Unable to save right now.");
      await expect(page.getByLabel("What needs doing?")).toHaveValue("Pending task");
      await expect(page.getByLabel("Notes (optional)")).toHaveValue("Keep these notes");
      await expect(page.getByLabel("Priority", { exact: true })).toHaveValue("High");
      await expect(page.getByLabel("Due date", { exact: true })).toHaveValue("2028-02-01");
      await expect(page.getByLabel("Repeat")).toHaveValue("Weekly");
      await page.getByRole("button", { name: "+ Add task" }).click();
      await expect(page.locator(".task-card")).toContainText("Pending task");
    }
  });
}

test("canceling deletion keeps the task", async ({ page }) => {
  await page.getByLabel("What needs doing?").fill("Keep this task");
  await page.getByRole("button", { name: "+ Add task" }).click();

  const card = page.locator(".task-card");
  await expect(card).toHaveCount(1);
  page.once("dialog", (dialog) => dialog.dismiss());
  await card.getByRole("button", { name: 'Delete "Keep this task"' }).click();

  await expect(card).toHaveCount(1);
  await expect(card).toContainText("Keep this task");
});

test("canceling an edit discards unsaved changes", async ({ page }) => {
  await page.getByLabel("What needs doing?").fill("Original title");
  await page.getByLabel("Notes (optional)").fill("Original notes");
  await page.getByRole("button", { name: "+ Add task" }).click();

  await page.locator(".task-card").getByRole("button", { name: 'Edit "Original title"' }).click();
  await page.getByLabel("What needs doing?").fill("Changed title");
  await page.getByLabel("Notes (optional)").fill("Changed notes");
  await page.getByRole("button", { name: "Cancel editing" }).click();

  await expect(page.locator(".task-card")).toContainText("Original title");
  await expect(page.locator(".task-card")).toContainText("Original notes");
  await expect(page.locator(".task-card")).not.toContainText("Changed title");
  await expect(page.getByRole("heading", { name: "Add a task" })).toBeVisible();
});

test("rejects a whitespace-only title without sending a request", async ({ page }) => {
  const posts: string[] = [];
  page.on("request", (request) => {
    if (request.method() === "POST" && request.url().endsWith("/api/tasks"))
      posts.push(request.url());
  });

  await page.getByLabel("What needs doing?").fill("   ");
  await page.getByRole("button", { name: "+ Add task" }).click();

  await expect(page.getByRole("alert")).toContainText("Please give your task a name.");
  expect(posts).toHaveLength(0);
  await expect(page.locator(".task-card")).toHaveCount(0);
});

test("sorts by due date with tied dates newest first and undated tasks last", async ({ page }) => {
  const entries = [
    { title: "No due date" },
    { title: "Older tied date", dueDate: "2028-02-03" },
    { title: "Earliest date", dueDate: "2028-02-01" },
    { title: "Newer tied date", dueDate: "2028-02-03" },
    { title: "Latest date", dueDate: "2028-02-04" },
  ];

  for (const [index, entry] of entries.entries()) {
    await page.getByLabel("What needs doing?").fill(entry.title);
    if (entry.dueDate)
      await page.getByLabel("Due date", { exact: true }).fill(entry.dueDate);
    await page.getByRole("button", { name: "+ Add task" }).click();
    await expect(page.locator(".task-card")).toHaveCount(index + 1);
  }

  await page.getByLabel("Sort by").selectOption("due");
  await expect(page.locator(".task-title")).toHaveText([
    "Earliest date",
    "Newer tied date",
    "Older tied date",
    "Latest date",
    "No due date",
  ]);
});

test("shows overdue counts and date labels against the browser's local date", async ({ page }) => {
  await page.clock.install({ time: new Date("2028-02-01T12:00:00") });
  await page.reload();
  await expect(page.locator("#loading")).toBeHidden();

  const entries = [
    { title: "Yesterday", dueDate: "2028-01-31" },
    { title: "Today", dueDate: "2028-02-01" },
    { title: "Tomorrow", dueDate: "2028-02-02" },
    { title: "Completed overdue", dueDate: "2028-01-30" },
  ];
  for (const [index, entry] of entries.entries()) {
    await page.getByLabel("What needs doing?").fill(entry.title);
    await page.getByLabel("Due date", { exact: true }).fill(entry.dueDate);
    await page.getByRole("button", { name: "+ Add task" }).click();
    await expect(page.locator(".task-card")).toHaveCount(index + 1);
  }

  await page.getByRole("checkbox", { name: 'Mark "Completed overdue" complete' }).click();

  await expect(page.locator("#overdue-count")).toHaveText("1");
  const yesterday = page.locator(".task-card").filter({ hasText: "Yesterday" });
  const today = page.locator(".task-card").filter({ hasText: "Today" });
  const tomorrow = page.locator(".task-card").filter({ hasText: "Tomorrow" });
  const completed = page.locator(".task-card").filter({ hasText: "Completed overdue" });
  await expect(yesterday.locator("time")).toHaveClass(/overdue/);
  await expect(today.locator("time")).not.toHaveClass(/overdue/);
  await expect(tomorrow.locator("time")).not.toHaveClass(/overdue/);
  await expect(completed.locator("time")).not.toHaveClass(/overdue/);
  await expect(today.locator("time")).toHaveAttribute("datetime", "2028-02-01");
  const expectedTodayLabel = await page.evaluate(() =>
    new Date("2028-02-01T00:00:00").toLocaleDateString(undefined, {
      month: "short", day: "numeric", year: "numeric",
    }));
  await expect(today.locator("time")).toContainText(expectedTodayLabel);
});
