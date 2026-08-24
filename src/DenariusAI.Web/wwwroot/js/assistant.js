(() => {
  const form = document.getElementById('assistantForm');
  if (!form) return;
  const input = document.getElementById('assistantQuestion');
  const messages = document.getElementById('assistantMessages');
  const counter = document.getElementById('assistantCounter');
  const submit = form.querySelector('button[type="submit"]');
  const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const history = [];

  const resize = () => { input.style.height = 'auto'; input.style.height = `${Math.min(input.scrollHeight, 140)}px`; counter.textContent = `${input.value.length} / 1000`; };
  const addMessage = (role, content, meta) => {
    messages.querySelector('.assistant-welcome')?.remove();
    const row = document.createElement('div'); row.className = `chat-row ${role}`;
    const avatar = document.createElement('span'); avatar.className = 'chat-avatar'; avatar.textContent = role === 'assistant' ? '✦' : 'EU';
    const body = document.createElement('div'); body.className = 'chat-body';
    const text = document.createElement('p'); text.textContent = content; body.appendChild(text);
    if (meta) { const small = document.createElement('small'); small.textContent = meta; body.appendChild(small); }
    row.append(avatar, body); messages.appendChild(row); messages.scrollTop = messages.scrollHeight; return row;
  };
  document.querySelectorAll('.assistant-suggestions button').forEach(button => button.addEventListener('click', () => { input.value = button.dataset.question || button.textContent.trim(); resize(); input.focus(); }));
  input.addEventListener('input', resize);
  input.addEventListener('keydown', event => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); form.requestSubmit(); } });
  form.addEventListener('submit', async event => {
    event.preventDefault(); const question = input.value.trim(); if (!question || submit.disabled) return;
    addMessage('user', question); input.value = ''; resize(); submit.disabled = true; input.disabled = true;
    const pending = addMessage('assistant', 'A analisar os seus dados…'); pending.classList.add('pending');
    try {
      const response = await fetch('/Assistant/Ask', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify({ question, history: history.slice(-10) }) });
      const result = await response.json(); pending.remove();
      if (!response.ok) throw new Error(result.error || 'Não foi possível concluir o pedido.');
      addMessage('assistant', result.answer, `Dados: ${result.dataFrom}–${result.dataTo} · ${result.transactionCount} movimentos · ${result.model}`);
      history.push({ role: 'user', content: question }, { role: 'assistant', content: result.answer });
    } catch (error) { pending.remove(); addMessage('assistant', error.message, 'Erro ao processar a pergunta'); }
    finally { submit.disabled = false; input.disabled = false; input.focus(); }
  });
  resize();
})();
