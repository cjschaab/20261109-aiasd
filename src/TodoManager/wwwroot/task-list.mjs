export function isOverdue(task, today) {
  return !task.isCompleted && Boolean(task.dueDate) && task.dueDate < today;
}

export function selectVisibleTasks(tasks, { filter, query, sort }) {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  const visible = tasks.filter((task) =>
    (filter === "all" || (filter === "completed" ? task.isCompleted : !task.isCompleted)) &&
    `${task.title} ${task.notes}`.toLocaleLowerCase().includes(normalizedQuery));
  const ranks = { High: 0, Medium: 1, Low: 2 };

  return visible.sort((a, b) => {
    if (sort === "due")
      return (a.dueDate || "9999-12-31").localeCompare(b.dueDate || "9999-12-31") || b.id - a.id;
    if (sort === "priority") return ranks[a.priority] - ranks[b.priority] || b.id - a.id;
    return b.id - a.id;
  });
}
