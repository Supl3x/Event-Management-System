(function () {
    const cfg = window.supabaseRealtimeConfig || {};
    if (!cfg.url || !cfg.anonKey || !window.supabase || !window.supabase.createClient) {
        return;
    }

    const client = window.supabase.createClient(cfg.url, cfg.anonKey);

    document.querySelectorAll("[data-competition-id]").forEach((node) => {
        const competitionId = node.getAttribute("data-competition-id");
        if (!competitionId) return;

        client.channel(`competition:${competitionId}`)
            .on("broadcast", { event: "seat_update" }, ({ payload }) => {
                const el = document.querySelector(`[data-seat-count="${competitionId}"]`);
                if (el && payload && payload.availableSeats !== undefined) {
                    el.textContent = String(payload.availableSeats);
                }
            })
            .subscribe();
    });

    client.channel("notifications")
        .on("broadcast", { event: "new_notification" }, ({ payload }) => {
            const toastBody = document.getElementById("liveNotificationToastBody");
            const toastEl = document.getElementById("liveNotificationToast");
            if (!toastBody || !toastEl || !payload) return;

            toastBody.textContent = payload.message || "You have a new notification.";
            const toast = window.bootstrap ? new window.bootstrap.Toast(toastEl) : null;
            if (toast) {
                toast.show();
            }
        })
        .subscribe();
})();
