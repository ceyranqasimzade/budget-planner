document.addEventListener("DOMContentLoaded", function () {
    const dateInput = document.querySelector("#transactionDate");
    if (dateInput) {
        flatpickr("#transactionDate", {
            locale: "az",
            dateFormat: "Y-m-d", 
            altInput: true,
            altFormat: "d.m.Y", 
            allowInput: true
        });
    }
});