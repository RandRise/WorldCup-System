function initNavigation() {
  const current = window.location.pathname.split("/").pop() || "index.html";
  document.querySelectorAll(".nav-link").forEach(function (link) {
    const href = link.getAttribute("href");
    if (href === current || (current === "" && href === "index.html")) {
      link.classList.add("active");
    }
  });
}

function initTaskLists() {
  document.querySelectorAll(".task-list[data-storage-key]").forEach(function (list) {
    const key = "worldcup-plan:" + list.dataset.storageKey;
    let saved = {};
    try {
      saved = JSON.parse(localStorage.getItem(key) || "{}");
    } catch (e) {
      saved = {};
    }

    let touched = false;

    list.querySelectorAll(".task-item").forEach(function (item) {
      const taskId = item.dataset.taskId;
      const checkbox = item.querySelector('input[type="checkbox"]');
      if (!checkbox || !taskId) return;

      if (Object.prototype.hasOwnProperty.call(saved, taskId)) {
        checkbox.checked = !!saved[taskId];
      } else if (checkbox.checked) {
        // HTML checked = documented project state (seed storage on first visit)
        saved[taskId] = true;
        touched = true;
      }

      if (checkbox.checked) {
        item.classList.add("done");
      } else {
        item.classList.remove("done");
      }

      checkbox.addEventListener("change", function () {
        saved[taskId] = checkbox.checked;
        localStorage.setItem(key, JSON.stringify(saved));
        item.classList.toggle("done", checkbox.checked);
        updatePhaseProgress(list);
        updateGlobalProgress();
      });
    });

    if (touched) {
      localStorage.setItem(key, JSON.stringify(saved));
    }

    updatePhaseProgress(list);
  });

  updateGlobalProgress();
}

function updatePhaseProgress(list) {
  const phase = list.closest(".phase");
  if (!phase) return;

  const items = list.querySelectorAll(".task-item");
  const done = list.querySelectorAll(".task-item.done").length;
  const total = items.length;
  const pct = total ? Math.round((done / total) * 100) : 0;

  const bar = phase.querySelector(".phase-progress-fill");
  const label = phase.querySelector(".phase-progress-label");
  if (bar) bar.style.width = pct + "%";
  if (label) label.textContent = done + " / " + total + " tasks (" + pct + "%)";

  phase.classList.toggle("done-phase", pct === 100);
  phase.classList.toggle("active-phase", pct > 0 && pct < 100);
}

function countStoredProgress() {
  const phaseKeys = [
    "phase-1", "phase-2", "phase-3", "phase-4",
    "phase-5", "phase-6", "phase-7", "phase-8"
  ];
  const taskCounts = {
    "phase-1": 9,
    "phase-2": 8,
    "phase-3": 7,
    "phase-4": 5,
    "phase-5": 8,
    "phase-6": 6,
    "phase-7": 8,
    "phase-8": 10
  };
  let total = 0;
  let done = 0;
  phaseKeys.forEach(function (phase) {
    total += taskCounts[phase];
    try {
      const saved = JSON.parse(localStorage.getItem("worldcup-plan:" + phase) || "{}");
      Object.keys(saved).forEach(function (id) {
        if (saved[id]) done++;
      });
    } catch (e) { /* ignore */ }
  });
  return { total: total, done: done, pct: total ? Math.round((done / total) * 100) : 0 };
}

function updateGlobalProgress() {
  const domItems = document.querySelectorAll(".task-list[data-storage-key] .task-item");
  let total, done, pct;

  if (domItems.length > 0) {
    const allDone = document.querySelectorAll(".task-list[data-storage-key] .task-item.done");
    total = domItems.length;
    done = allDone.length;
    pct = total ? Math.round((done / total) * 100) : 0;
  } else {
    const stored = countStoredProgress();
    total = stored.total;
    done = stored.done;
    pct = stored.pct;
  }

  document.querySelectorAll("[data-global-progress]").forEach(function (el) {
    el.textContent = pct + "%";
  });

  document.querySelectorAll("[data-global-progress-bar]").forEach(function (el) {
    el.style.width = pct + "%";
  });

  document.querySelectorAll("[data-global-done]").forEach(function (el) {
    el.textContent = done;
  });

  document.querySelectorAll("[data-global-total]").forEach(function (el) {
    el.textContent = total;
  });
}

function resetAllTasks() {
  if (!confirm("Reset checklist overrides? Documented completed tasks will re-apply from the HTML defaults.")) return;
  Object.keys(localStorage).forEach(function (key) {
    if (key.startsWith("worldcup-plan:")) {
      localStorage.removeItem(key);
    }
  });
  location.reload();
}

document.addEventListener("DOMContentLoaded", function () {
  initNavigation();
  initTaskLists();

  const resetBtn = document.getElementById("reset-progress");
  if (resetBtn) {
    resetBtn.addEventListener("click", resetAllTasks);
  }
});
