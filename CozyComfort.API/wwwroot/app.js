const roles = ["Manufacturer", "Distributor", "Seller", "Admin"];
const orderStatuses = ["Pending", "Approved", "Processing", "Shipped", "Delivered", "Cancelled"];
const inventoryOwnerTypes = ["Manufacturer", "Distributor", "Seller"];

const state = {
    token: localStorage.getItem("cozycomfort.token") ?? "",
    user: null,
    blankets: [],
    orders: [],
    inventory: [],
    notifications: [],
    users: [],
    distributorOrders: [],
    assignedSellers: []
};

const elements = {
    sessionStatus: document.getElementById("sessionStatus"),
    feedback: document.getElementById("feedback"),
    logoutButton: document.getElementById("logoutButton"),
    refreshDashboardButton: document.getElementById("refreshDashboardButton"),
    profileSummary: document.getElementById("profileSummary"),
    blanketsSummary: document.getElementById("blanketsSummary"),
    blanketsTableBody: document.getElementById("blanketsTableBody"),
    blanketsActionsHeader: document.getElementById("blanketsActionsHeader"),
    blanketForm: document.getElementById("blanketForm"),
    blanketFormResetButton: document.getElementById("blanketFormResetButton"),
    ordersSummary: document.getElementById("ordersSummary"),
    ordersTableBody: document.getElementById("ordersTableBody"),
    createOrderForm: document.getElementById("createOrderForm"),
    updateOrderForm: document.getElementById("updateOrderForm"),
    orderItemsContainer: document.getElementById("orderItemsContainer"),
    orderItemTemplate: document.getElementById("orderItemTemplate"),
    addOrderItemButton: document.getElementById("addOrderItemButton"),
    inventorySummary: document.getElementById("inventorySummary"),
    inventoryTableBody: document.getElementById("inventoryTableBody"),
    stockCheckForm: document.getElementById("stockCheckForm"),
    stockCheckResult: document.getElementById("stockCheckResult"),
    inventoryUpdateForm: document.getElementById("inventoryUpdateForm"),
    inventoryTransferForm: document.getElementById("inventoryTransferForm"),
    productionCapacityForm: document.getElementById("productionCapacityForm"),
    productionCapacityResult: document.getElementById("productionCapacityResult"),
    leadTimeForm: document.getElementById("leadTimeForm"),
    leadTimeResult: document.getElementById("leadTimeResult"),
    productionStatusForm: document.getElementById("productionStatusForm"),
    assignedSellersTableBody: document.getElementById("assignedSellersTableBody"),
    distributorOrdersTableBody: document.getElementById("distributorOrdersTableBody"),
    fulfillOrderForm: document.getElementById("fulfillOrderForm"),
    usersTableBody: document.getElementById("usersTableBody"),
    userRoleForm: document.getElementById("userRoleForm"),
    notificationsList: document.getElementById("notificationsList")
};

initialize();

function initialize() {
    seedStaticOptions();
    wireEvents();
    resetOrderItems();
    renderSession();
    renderAll();

    if (state.token) {
        loadDashboard(true);
    }
}

function wireEvents() {
    document.getElementById("loginForm").addEventListener("submit", handleLogin);
    document.getElementById("registerForm").addEventListener("submit", handleRegister);
    elements.logoutButton.addEventListener("click", logout);
    elements.refreshDashboardButton.addEventListener("click", () => loadDashboard(false));
    elements.blanketForm.addEventListener("submit", handleBlanketSubmit);
    elements.blanketFormResetButton.addEventListener("click", resetBlanketForm);
    elements.addOrderItemButton.addEventListener("click", addOrderItemRow);
    elements.createOrderForm.addEventListener("submit", handleCreateOrder);
    elements.updateOrderForm.addEventListener("submit", handleUpdateOrder);
    elements.stockCheckForm.addEventListener("submit", handleStockCheck);
    elements.inventoryUpdateForm.addEventListener("submit", handleInventoryUpdate);
    elements.inventoryTransferForm.addEventListener("submit", handleInventoryTransfer);
    elements.productionCapacityForm.addEventListener("submit", handleProductionCapacity);
    elements.leadTimeForm.addEventListener("submit", handleLeadTime);
    elements.productionStatusForm.addEventListener("submit", handleProductionStatus);
    elements.fulfillOrderForm.addEventListener("submit", handleFulfillOrder);
    elements.userRoleForm.addEventListener("submit", handleUserRoleUpdate);
}

function seedStaticOptions() {
    populateSelect(elements.updateOrderForm.elements.status, orderStatuses, { includeBlank: true, blankLabel: "Keep current status" });
    populateSelect(elements.userRoleForm.elements.role, roles);

    [
        elements.stockCheckForm.elements.ownerType,
        elements.inventoryUpdateForm.elements.ownerType,
        elements.inventoryTransferForm.elements.fromOwnerType,
        elements.inventoryTransferForm.elements.toOwnerType
    ].forEach((select) => populateSelect(select, inventoryOwnerTypes));
}

async function handleLogin(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const payload = {
        email: form.elements.email.value.trim(),
        password: form.elements.password.value
    };

    const response = await apiRequest("/api/auth/login", {
        method: "POST",
        body: JSON.stringify(payload)
    });

    persistSession(response);
    form.reset();
    setFeedback(`Logged in as ${response.user.fullName}.`, false);
    await loadDashboard(false);
}

async function handleRegister(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const assignedDistributorId = form.elements.assignedDistributorId.value.trim();
    const payload = {
        fullName: form.elements.fullName.value.trim(),
        email: form.elements.email.value.trim(),
        password: form.elements.password.value,
        assignedDistributorId: assignedDistributorId || null
    };

    const response = await apiRequest("/api/auth/register", {
        method: "POST",
        body: JSON.stringify(payload)
    });

    persistSession(response);
    form.reset();
    setFeedback(`Registration complete. Welcome ${response.user.fullName}.`, false);
    await loadDashboard(false);
}

async function loadDashboard(isRestoringSession) {
    if (!state.token) {
        if (!isRestoringSession) {
            setFeedback("Login to load the dashboard.", true);
        }
        return;
    }

    try {
        const commonRequests = [
            apiRequest("/api/users/profile"),
            apiRequest("/api/blankets"),
            apiRequest("/api/orders"),
            apiRequest("/api/inventory"),
            apiRequest("/api/notifications")
        ];

        const [profile, blankets, orders, inventory, notifications] = await Promise.all(commonRequests);

        state.user = profile;
        state.blankets = Array.isArray(blankets) ? blankets : [];
        state.orders = Array.isArray(orders) ? orders : [];
        state.inventory = Array.isArray(inventory) ? inventory : [];
        state.notifications = Array.isArray(notifications) ? notifications : [];
        state.users = [];
        state.distributorOrders = [];
        state.assignedSellers = [];

        const roleRequests = [];

        if (isRole("Admin")) {
            roleRequests.push(apiRequest("/api/users").then((users) => { state.users = Array.isArray(users) ? users : []; }));
        }

        if (isRole("Distributor")) {
            roleRequests.push(apiRequest("/api/distributors/assigned-sellers").then((sellers) => { state.assignedSellers = Array.isArray(sellers) ? sellers : []; }));
            roleRequests.push(apiRequest("/api/distributors/orders").then((ordersResponse) => { state.distributorOrders = Array.isArray(ordersResponse) ? ordersResponse : []; }));
        }

        await Promise.all(roleRequests);
        renderSession();
        renderAll();

        if (!isRestoringSession) {
            setFeedback("Dashboard refreshed.", false);
        }
    } catch (error) {
        if (isRestoringSession) {
            logout(false);
            setFeedback("Stored session expired. Please login again.", true);
        } else {
            setFeedback(error.message, true);
        }
    }
}

async function handleBlanketSubmit(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    const blanketId = form.elements.blanketId.value.trim();
    const payload = {
        modelName: form.elements.modelName.value.trim(),
        material: form.elements.material.value.trim(),
        size: form.elements.size.value.trim(),
        color: form.elements.color.value.trim(),
        price: Number(form.elements.price.value),
        productionCapacity: Number(form.elements.productionCapacity.value),
        currentStock: Number(form.elements.currentStock.value)
    };

    const response = await apiRequest(blanketId ? `/api/blankets/${blanketId}` : "/api/blankets", {
        method: blanketId ? "PUT" : "POST",
        body: JSON.stringify(payload)
    });

    resetBlanketForm();
    setFeedback(`Blanket ${response.modelName} saved.`, false);
    await loadDashboard(false);
}

async function handleCreateOrder(event) {
    event.preventDefault();
    requireAuth();

    const payload = {
        customerName: event.currentTarget.elements.customerName.value.trim(),
        deliveryAddress: event.currentTarget.elements.deliveryAddress.value.trim(),
        items: collectOrderItems()
    };

    await apiRequest("/api/orders", {
        method: "POST",
        body: JSON.stringify(payload)
    });

    event.currentTarget.reset();
    resetOrderItems();
    setFeedback("Order created.", false);
    await loadDashboard(false);
}

async function handleUpdateOrder(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    const payload = {};

    if (form.elements.status.value) {
        payload.status = form.elements.status.value;
    }

    if (form.elements.notes.value.trim()) {
        payload.notes = form.elements.notes.value.trim();
    }

    if (form.elements.deliveryAddress.value.trim()) {
        payload.deliveryAddress = form.elements.deliveryAddress.value.trim();
    }

    await apiRequest(`/api/orders/${form.elements.orderId.value.trim()}`, {
        method: "PUT",
        body: JSON.stringify(payload)
    });

    form.reset();
    populateSelect(form.elements.status, orderStatuses, { includeBlank: true, blankLabel: "Keep current status" });
    setFeedback("Order updated.", false);
    await loadDashboard(false);
}

async function handleStockCheck(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    const params = new URLSearchParams({
        blanketId: form.elements.blanketId.value,
        ownerType: form.elements.ownerType.value,
        ownerUserId: form.elements.ownerUserId.value.trim()
    });

    const response = await apiRequest(`/api/inventory/check?${params.toString()}`);
    elements.stockCheckResult.textContent = JSON.stringify(response, null, 2);
    setFeedback("Stock check completed.", false);
}

async function handleInventoryUpdate(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    await apiRequest("/api/inventory", {
        method: "PUT",
        body: JSON.stringify({
            blanketId: form.elements.blanketId.value,
            ownerType: form.elements.ownerType.value,
            ownerUserId: form.elements.ownerUserId.value.trim(),
            quantity: Number(form.elements.quantity.value)
        })
    });

    form.reset();
    refillDynamicSelects();
    setFeedback("Inventory updated.", false);
    await loadDashboard(false);
}

async function handleInventoryTransfer(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    await apiRequest("/api/inventory/transfer", {
        method: "POST",
        body: JSON.stringify({
            blanketId: form.elements.blanketId.value,
            fromOwnerType: form.elements.fromOwnerType.value,
            fromOwnerUserId: form.elements.fromOwnerUserId.value.trim(),
            toOwnerType: form.elements.toOwnerType.value,
            toOwnerUserId: form.elements.toOwnerUserId.value.trim(),
            quantity: Number(form.elements.quantity.value)
        })
    });

    form.reset();
    refillDynamicSelects();
    setFeedback("Inventory transferred.", false);
    await loadDashboard(false);
}

async function handleProductionCapacity(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    const params = new URLSearchParams({
        blanketId: form.elements.blanketId.value,
        requestedQuantity: form.elements.requestedQuantity.value
    });

    const response = await apiRequest(`/api/manufacturers/production-capacity?${params.toString()}`);
    elements.productionCapacityResult.textContent = JSON.stringify(response, null, 2);
    setFeedback("Production capacity checked.", false);
}

async function handleLeadTime(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    const params = new URLSearchParams({
        blanketId: form.elements.blanketId.value,
        requestedQuantity: form.elements.requestedQuantity.value
    });

    const response = await apiRequest(`/api/manufacturers/lead-time?${params.toString()}`);
    elements.leadTimeResult.textContent = JSON.stringify(response, null, 2);
    setFeedback("Lead time generated.", false);
}

async function handleProductionStatus(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    await apiRequest(`/api/manufacturers/production-status/${form.elements.blanketId.value}`, {
        method: "PUT",
        body: JSON.stringify({
            currentStock: Number(form.elements.currentStock.value),
            productionCapacity: Number(form.elements.productionCapacity.value)
        })
    });

    form.reset();
    refillDynamicSelects();
    setFeedback("Production status updated.", false);
    await loadDashboard(false);
}

async function handleFulfillOrder(event) {
    event.preventDefault();
    requireAuth();

    const orderId = event.currentTarget.elements.orderId.value.trim();
    await apiRequest(`/api/distributors/orders/${orderId}/fulfill`, { method: "POST" });

    event.currentTarget.reset();
    setFeedback("Distributor order fulfilled.", false);
    await loadDashboard(false);
}

async function handleUserRoleUpdate(event) {
    event.preventDefault();
    requireAuth();

    const form = event.currentTarget;
    const assignedDistributorId = form.elements.assignedDistributorId.value.trim();
    await apiRequest(`/api/users/${form.elements.userId.value}/role`, {
        method: "PUT",
        body: JSON.stringify({
            role: form.elements.role.value,
            assignedDistributorId: assignedDistributorId || null
        })
    });

    setFeedback("User role updated.", false);
    await loadDashboard(false);
}

function persistSession(response) {
    state.token = response.token;
    state.user = response.user;
    localStorage.setItem("cozycomfort.token", response.token);
}

function logout(showFeedback = true) {
    state.token = "";
    state.user = null;
    state.blankets = [];
    state.orders = [];
    state.inventory = [];
    state.notifications = [];
    state.users = [];
    state.distributorOrders = [];
    state.assignedSellers = [];
    localStorage.removeItem("cozycomfort.token");
    renderSession();
    renderAll();
    if (showFeedback) {
        setFeedback("Logged out.", false);
    }
}

async function apiRequest(path, options = {}) {
    const headers = new Headers(options.headers || {});
    if (options.body && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }
    if (state.token) {
        headers.set("Authorization", `Bearer ${state.token}`);
    }

    const response = await fetch(path, { ...options, headers });
    if (response.status === 204) {
        return null;
    }

    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("application/json") ? await response.json() : await response.text();

    if (!response.ok) {
        throw new Error(extractError(payload));
    }

    return payload;
}

function extractError(payload) {
    if (typeof payload === "string" && payload.trim()) {
        return payload;
    }

    if (payload && typeof payload === "object") {
        if (typeof payload.message === "string" && payload.message.trim()) {
            return payload.message;
        }
        if (typeof payload.title === "string" && payload.title.trim()) {
            return payload.title;
        }
    }

    return "Request failed.";
}

function renderAll() {
    renderProfile();
    renderBlankets();
    renderOrders();
    renderInventory();
    renderManufacturerTools();
    renderDistributorWorkspace();
    renderUsers();
    renderNotifications();
    refillDynamicSelects();
    updateRoleVisibility();
}

function renderSession() {
    if (state.user) {
        elements.sessionStatus.textContent = `${state.user.fullName} (${state.user.role})`;
        elements.logoutButton.disabled = false;
        elements.refreshDashboardButton.disabled = false;
    } else {
        elements.sessionStatus.textContent = "Signed out";
        elements.logoutButton.disabled = true;
        elements.refreshDashboardButton.disabled = true;
    }
}

function renderProfile() {
    if (!state.user) {
        elements.profileSummary.className = "summary-grid empty-state";
        elements.profileSummary.textContent = "Login to load your profile.";
        return;
    }

    elements.profileSummary.className = "summary-grid";
    elements.profileSummary.innerHTML = [
        summaryCard("Full name", state.user.fullName),
        summaryCard("Email", state.user.email),
        summaryCard("Role", state.user.role),
        summaryCard("User ID", `<span class="code">${escapeHtml(state.user.id)}</span>`, true),
        summaryCard("Assigned distributor", state.user.assignedDistributorId ? `<span class="code">${escapeHtml(state.user.assignedDistributorId)}</span>` : "Not assigned", true),
        summaryCard("Created", formatDateTime(state.user.createdAtUtc))
    ].join("");
}

function renderBlankets() {
    elements.blanketsActionsHeader.classList.toggle("hidden", !canManageBlankets());

    elements.blanketsSummary.innerHTML = state.blankets.length
        ? [
            summaryPill(`Models: ${state.blankets.length}`),
            summaryPill(`Stock units: ${sumBy(state.blankets, (item) => item.currentStock)}`),
            summaryPill(`Total capacity: ${sumBy(state.blankets, (item) => item.productionCapacity)}`)
        ].join("")
        : "";

    if (!state.blankets.length) {
        elements.blanketsTableBody.innerHTML = singleRow("No blankets loaded.", canManageBlankets() ? 8 : 7);
        return;
    }

    elements.blanketsActionsHeader.classList.toggle("hidden", !canManageBlankets());
    elements.blanketsTableBody.innerHTML = state.blankets.map((blanket) => {
        const actions = canManageBlankets()
            ? `<div class="button-row"><button type="button" class="secondary" data-action="edit-blanket" data-id="${escapeHtml(blanket.id)}">Edit</button><button type="button" class="ghost" data-action="delete-blanket" data-id="${escapeHtml(blanket.id)}">Delete</button></div>`
            : "";

        return `
            <tr>
                <td>${escapeHtml(blanket.modelName)}</td>
                <td>${escapeHtml(blanket.material)}</td>
                <td>${escapeHtml(blanket.size)}</td>
                <td>${escapeHtml(blanket.color)}</td>
                <td>${formatCurrency(blanket.price)}</td>
                <td>${escapeHtml(String(blanket.currentStock))}</td>
                <td>${escapeHtml(String(blanket.productionCapacity))}</td>
                ${canManageBlankets() ? `<td>${actions}</td>` : ""}
            </tr>
        `;
    }).join("");

    elements.blanketsTableBody.querySelectorAll("[data-action='edit-blanket']").forEach((button) => {
        button.addEventListener("click", () => fillBlanketForm(button.dataset.id));
    });

    elements.blanketsTableBody.querySelectorAll("[data-action='delete-blanket']").forEach((button) => {
        button.addEventListener("click", () => deleteBlanket(button.dataset.id));
    });
}

function renderOrders() {
    elements.ordersSummary.innerHTML = state.orders.length
        ? [
            summaryPill(`Orders: ${state.orders.length}`),
            summaryPill(`Revenue: ${formatCurrency(sumBy(state.orders, (order) => order.totalAmount))}`),
            summaryPill(`Pending: ${state.orders.filter((order) => order.status === "Pending").length}`)
        ].join("")
        : "";

    if (!state.orders.length) {
        elements.ordersTableBody.innerHTML = singleRow("No orders loaded.", 7);
        return;
    }

    elements.ordersTableBody.innerHTML = state.orders.map((order) => `
        <tr>
            <td>${escapeHtml(order.customerName)}<div class="code">${escapeHtml(order.id)}</div></td>
            <td>${renderStatusTag(order.status)}</td>
            <td>${formatCurrency(order.totalAmount)}</td>
            <td>${escapeHtml(order.sellerName)}</td>
            <td>${escapeHtml(order.distributorName)}</td>
            <td>${escapeHtml(String(order.estimatedLeadTimeDays))} days</td>
            <td>${escapeHtml(order.items.map((item) => `${item.blanketModelName} × ${item.quantity}`).join(", "))}</td>
        </tr>
    `).join("");
}

function renderInventory() {
    elements.inventorySummary.innerHTML = state.inventory.length
        ? [
            summaryPill(`Records: ${state.inventory.length}`),
            summaryPill(`Units tracked: ${sumBy(state.inventory, (item) => item.quantity)}`)
        ].join("")
        : "";

    if (!state.inventory.length) {
        elements.inventoryTableBody.innerHTML = singleRow("No inventory loaded.", 5);
        return;
    }

    elements.inventoryTableBody.innerHTML = state.inventory.map((item) => `
        <tr>
            <td>${escapeHtml(item.blanketModelName)}</td>
            <td>${escapeHtml(item.ownerType)}</td>
            <td>${escapeHtml(item.ownerName)}<div class="code">${escapeHtml(item.ownerUserId)}</div></td>
            <td>${escapeHtml(String(item.quantity))}</td>
            <td>${formatDateTime(item.updatedAtUtc)}</td>
        </tr>
    `).join("");
}

function renderManufacturerTools() {
    elements.productionCapacityResult.textContent ||= "No capacity check performed.";
    elements.leadTimeResult.textContent ||= "No lead time request performed.";
}

function renderDistributorWorkspace() {
    elements.assignedSellersTableBody.innerHTML = state.assignedSellers.length
        ? state.assignedSellers.map((seller) => `
            <tr>
                <td>${escapeHtml(seller.sellerName)}</td>
                <td>${escapeHtml(seller.sellerEmail)}</td>
                <td class="code">${escapeHtml(seller.sellerId)}</td>
            </tr>
        `).join("")
        : singleRow("No assigned sellers loaded.", 3);

    elements.distributorOrdersTableBody.innerHTML = state.distributorOrders.length
        ? state.distributorOrders.map((order) => `
            <tr>
                <td class="code">${escapeHtml(order.id)}</td>
                <td>${escapeHtml(order.customerName)}</td>
                <td>${renderStatusTag(order.status)}</td>
                <td>${formatCurrency(order.totalAmount)}</td>
            </tr>
        `).join("")
        : singleRow("No distributor orders loaded.", 4);
}

function renderUsers() {
    elements.usersTableBody.innerHTML = state.users.length
        ? state.users.map((user) => `
            <tr>
                <td>${escapeHtml(user.fullName)}</td>
                <td>${escapeHtml(user.email)}</td>
                <td>${escapeHtml(user.role)}</td>
                <td class="code">${escapeHtml(user.assignedDistributorId || "—")}</td>
                <td class="code">${escapeHtml(user.id)}</td>
            </tr>
        `).join("")
        : singleRow("No users loaded.", 5);

    populateUserOptions();
}

function renderNotifications() {
    if (!state.user) {
        elements.notificationsList.className = "notification-list empty-state";
        elements.notificationsList.textContent = "Login to load notifications.";
        return;
    }

    elements.notificationsList.className = "notification-list";

    if (!state.notifications.length) {
        elements.notificationsList.innerHTML = '<div class="empty-state">No notifications available.</div>';
        return;
    }

    elements.notificationsList.innerHTML = state.notifications.map((notification) => `
        <article class="notification-card ${notification.isRead ? "" : "unread"}">
            <div class="button-row">
                ${renderNotificationType(notification.type)}
                <span class="tag ${notification.isRead ? "info" : "warning"}">${notification.isRead ? "Read" : "Unread"}</span>
            </div>
            <div>
                <h3>${escapeHtml(notification.title)}</h3>
                <p>${escapeHtml(notification.message)}</p>
            </div>
            <div class="button-row">
                <span>${formatDateTime(notification.createdAtUtc)}</span>
                ${notification.isRead ? "" : `<button type="button" class="secondary" data-action="read-notification" data-id="${escapeHtml(notification.id)}">Mark as read</button>`}
            </div>
        </article>
    `).join("");

    elements.notificationsList.querySelectorAll("[data-action='read-notification']").forEach((button) => {
        button.addEventListener("click", () => markNotificationAsRead(button.dataset.id));
    });
}

function updateRoleVisibility() {
    document.querySelectorAll("[data-role-guard]").forEach((element) => {
        const allowedRoles = element.dataset.roleGuard.split(",").map((value) => value.trim());
        element.classList.toggle("hidden", !state.user || !allowedRoles.includes(state.user.role));
    });
}

function refillDynamicSelects() {
    const blanketOptions = state.blankets.map((blanket) => ({ value: blanket.id, label: `${blanket.modelName} (${blanket.color}/${blanket.size})` }));
    document.querySelectorAll("select[name='blanketId']").forEach((select) => populateSelect(select, blanketOptions, { includeBlank: false, keepSelection: true }));
    document.querySelectorAll(".order-item-row select[name='blanketId']").forEach((select) => populateSelect(select, blanketOptions, { includeBlank: false, keepSelection: true }));
}

function addOrderItemRow() {
    const fragment = elements.orderItemTemplate.content.cloneNode(true);
    const row = fragment.querySelector(".order-item-row");
    const select = row.querySelector("select[name='blanketId']");
    populateSelect(select, state.blankets.map((blanket) => ({ value: blanket.id, label: `${blanket.modelName} (${blanket.color}/${blanket.size})` })));
    row.querySelector(".remove-order-item-button").addEventListener("click", () => {
        row.remove();
        if (!elements.orderItemsContainer.children.length) {
            addOrderItemRow();
        }
    });
    elements.orderItemsContainer.appendChild(fragment);
}

function resetOrderItems() {
    elements.orderItemsContainer.innerHTML = "";
    addOrderItemRow();
}

function collectOrderItems() {
    const items = Array.from(elements.orderItemsContainer.querySelectorAll(".order-item-row")).map((row) => ({
        blanketId: row.querySelector("select[name='blanketId']").value,
        quantity: Number(row.querySelector("input[name='quantity']").value)
    })).filter((item) => item.blanketId && Number.isFinite(item.quantity) && item.quantity > 0);

    if (!items.length) {
        throw new Error("Add at least one valid order item.");
    }

    return items;
}

function populateSelect(select, items, options = {}) {
    if (!select) {
        return;
    }

    const previousValue = options.keepSelection ? select.value : undefined;
    select.innerHTML = "";

    if (options.includeBlank) {
        const blankOption = document.createElement("option");
        blankOption.value = "";
        blankOption.textContent = options.blankLabel || "Select";
        select.appendChild(blankOption);
    }

    items.forEach((item) => {
        const option = document.createElement("option");
        if (typeof item === "string") {
            option.value = item;
            option.textContent = item;
        } else {
            option.value = item.value;
            option.textContent = item.label;
        }
        select.appendChild(option);
    });

    if (options.keepSelection && previousValue && Array.from(select.options).some((option) => option.value === previousValue)) {
        select.value = previousValue;
    }
}

function populateUserOptions() {
    const userSelect = elements.userRoleForm.elements.userId;
    const options = state.users.map((user) => ({ value: user.id, label: `${user.fullName} (${user.role})` }));
    populateSelect(userSelect, options, { keepSelection: true });
}

function fillBlanketForm(blanketId) {
    const blanket = state.blankets.find((item) => item.id === blanketId);
    if (!blanket) {
        return;
    }

    elements.blanketForm.elements.blanketId.value = blanket.id;
    elements.blanketForm.elements.modelName.value = blanket.modelName;
    elements.blanketForm.elements.material.value = blanket.material;
    elements.blanketForm.elements.size.value = blanket.size;
    elements.blanketForm.elements.color.value = blanket.color;
    elements.blanketForm.elements.price.value = blanket.price;
    elements.blanketForm.elements.productionCapacity.value = blanket.productionCapacity;
    elements.blanketForm.elements.currentStock.value = blanket.currentStock;
    window.scrollTo({ top: elements.blanketForm.getBoundingClientRect().top + window.scrollY - 90, behavior: "smooth" });
}

function resetBlanketForm() {
    elements.blanketForm.reset();
    elements.blanketForm.elements.blanketId.value = "";
}

async function deleteBlanket(blanketId) {
    requireAuth();
    const blanket = state.blankets.find((item) => item.id === blanketId);
    const confirmed = window.confirm(`Delete ${blanket?.modelName || "this blanket"}?`);
    if (!confirmed) {
        return;
    }

    await apiRequest(`/api/blankets/${blanketId}`, { method: "DELETE" });
    setFeedback("Blanket deleted.", false);
    await loadDashboard(false);
}

async function markNotificationAsRead(notificationId) {
    requireAuth();
    await apiRequest(`/api/notifications/${notificationId}/read`, { method: "PATCH" });
    setFeedback("Notification marked as read.", false);
    await loadDashboard(false);
}

function requireAuth() {
    if (!state.token) {
        throw new Error("Login is required.");
    }
}

function isRole(role) {
    return state.user?.role === role;
}

function canManageBlankets() {
    return isRole("Manufacturer") || isRole("Admin");
}

function setFeedback(message, isError) {
    elements.feedback.textContent = message;
    elements.feedback.className = `feedback ${isError ? "error" : "success"}`;
}

function summaryCard(label, value, isMarkup = false) {
    return `<div class="summary-card"><strong>${escapeHtml(label)}</strong><p>${isMarkup ? value : escapeHtml(value)}</p></div>`;
}

function summaryPill(text) {
    return `<span class="summary-pill">${escapeHtml(text)}</span>`;
}

function singleRow(message, colSpan) {
    return `<tr><td colspan="${colSpan}" class="empty-state">${escapeHtml(message)}</td></tr>`;
}

function renderStatusTag(status) {
    const lower = String(status).toLowerCase();
    const tone = lower === "delivered"
        ? "success"
        : lower === "cancelled"
            ? "danger"
            : lower === "pending"
                ? "warning"
                : "info";

    return `<span class="tag ${tone}">${escapeHtml(String(status))}</span>`;
}

function renderNotificationType(type) {
    const tone = type === "StockAlert" ? "warning" : type === "System" ? "info" : "success";
    return `<span class="tag ${tone}">${escapeHtml(type)}</span>`;
}

function formatDateTime(value) {
    if (!value) {
        return "—";
    }
    return new Intl.DateTimeFormat(undefined, {
        dateStyle: "medium",
        timeStyle: "short"
    }).format(new Date(value));
}

function formatCurrency(value) {
    return new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "USD",
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(Number(value || 0));
}

function sumBy(items, selector) {
    return items.reduce((total, item) => total + Number(selector(item) || 0), 0);
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

window.addEventListener("unhandledrejection", (event) => {
    setFeedback(event.reason?.message || "Unexpected error.", true);
});
