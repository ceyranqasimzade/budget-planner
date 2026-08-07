function toggleAiChat() {
    const container = document.getElementById('aiChatContainer');
    if (!container) return;

    container.classList.toggle('d-none');
    if (!container.classList.contains('d-none')) {
        const input = document.getElementById('aiUserInput');
        if (input) input.focus();
    }
}

function handleAiKeyPress(e) {
    if (e.key === 'Enter') {
        sendAiMessage();
    }
}

function sendAiMessage() {
    const input = document.getElementById('aiUserInput');
    const message = input ? input.value.trim() : '';
    if (!message) return;

    const chatMessages = document.getElementById('aiChatMessages');

    // 1. İstifadəçi mesajını ekrana əlavə edirik
    const userMsgDiv = document.createElement('div');
    userMsgDiv.className = 'ai-msg user';
    userMsgDiv.innerText = message;
    chatMessages.appendChild(userMsgDiv);

    input.value = '';
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // 2. Yüklənir (Loading) indikatoru
    const loadingDiv = document.createElement('div');
    loadingDiv.className = 'ai-msg bot';
    loadingDiv.id = 'aiLoadingMsg';
    loadingDiv.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-2"></i>Cavab hazırlanır...';
    chatMessages.appendChild(loadingDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // 3. Controller-ə sorğu göndərilir
    fetch('/Ai/AskAi', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ message: message })
    })
        .then(res => res.json())
        .then(data => {
            const loadingMsg = document.getElementById('aiLoadingMsg');
            if (loadingMsg) loadingMsg.remove();

            const botMsgDiv = document.createElement('div');
            botMsgDiv.className = 'ai-msg bot';

            if (data.success) {
                botMsgDiv.innerText = data.answer;
            } else {
                botMsgDiv.innerText = data.message || "Xəta baş verdi.";
            }

            chatMessages.appendChild(botMsgDiv);
            chatMessages.scrollTop = chatMessages.scrollHeight;
        })
        .catch(err => {
            console.error("AI Error:", err);
            const loadingMsg = document.getElementById('aiLoadingMsg');
            if (loadingMsg) loadingMsg.remove();

            const botMsgDiv = document.createElement('div');
            botMsgDiv.className = 'ai-msg bot';
            botMsgDiv.innerText = "Şəbəkə xətası yarandı. Lütfən yenidən cəhd edin.";
            chatMessages.appendChild(botMsgDiv);
            chatMessages.scrollTop = chatMessages.scrollHeight;
        });
}