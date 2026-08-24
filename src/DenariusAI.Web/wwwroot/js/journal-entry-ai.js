(() => {
  const toggle = document.getElementById('toggle-movement-ai');
  if (!toggle) return;
  const chat = document.getElementById('movement-ai-chat');
  const chatForm = document.getElementById('movement-ai-form');
  const input = document.getElementById('movement-ai-input');
  const messages = document.getElementById('movement-ai-messages');
  const history = [];
  const token = document.querySelector('#journal-entry-form input[name="__RequestVerificationToken"]')?.value;
  toggle.addEventListener('click', () => { chat.hidden = !chat.hidden; toggle.textContent = chat.hidden ? 'Iniciar conversa' : 'Fechar conversa'; if (!chat.hidden) input.focus(); });
  const add = (role, text) => { const bubble = document.createElement('div'); bubble.className = `movement-ai-message ${role}`; bubble.textContent = text; messages.appendChild(bubble); messages.scrollTop = messages.scrollHeight; return bubble; };
  const setValue = (selector, value) => { const field = document.querySelector(selector); if (!field) return; field.value = value ?? ''; field.dispatchEvent(new Event('change', { bubbles: true })); field.dispatchEvent(new Event('input', { bubbles: true })); };
  const applySuggestion = suggestion => {
    setValue('#Date', suggestion.date); setValue('#Description', suggestion.description); setValue('#Reference', suggestion.reference); setValue('#Notes', suggestion.notes); setValue('#BudgetId', suggestion.budgetId);
    const body = document.getElementById('journal-lines'); const template = document.getElementById('journal-line-template'); body.innerHTML = '';
    suggestion.lines.forEach((line, index) => {
      body.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__index__', index)); const row = body.lastElementChild;
      row.querySelector('.line-account').value = line.accountId; row.querySelector('[name$=".CategoryId"]').value = line.categoryId ?? ''; row.querySelector('[name$=".Description"]').value = line.description ?? ''; row.querySelector('.line-debit').value = line.debit; row.querySelector('.line-credit').value = line.credit;
    });
    body.dispatchEvent(new Event('change', { bubbles: true })); body.querySelector('.line-debit')?.dispatchEvent(new Event('input', { bubbles: true }));
    document.getElementById('journal-entry-form').scrollIntoView({ behavior: 'smooth', block: 'start' });
  };
  chatForm.addEventListener('submit', async event => {
    event.preventDefault(); const text = input.value.trim(); if (!text) return; add('user', text); input.value = ''; const button = chatForm.querySelector('button'); button.disabled = true; input.disabled = true; const pending = add('assistant pending', 'A interpretar…');
    try {
      const response = await fetch('/JournalEntries/Suggest', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify({ message: text, history: history.slice(-10) }) });
      const result = await response.json(); pending.remove(); if (!response.ok) throw new Error(result.error || 'Não foi possível criar a sugestão.');
      add('assistant', result.message); history.push({ role: 'user', content: text }, { role: 'assistant', content: result.message });
      if (result.isComplete && result.suggestion) { applySuggestion(result.suggestion); add('assistant success', 'Os campos foram preenchidos. Reveja as contas, categorias e valores antes de guardar.'); }
    } catch (error) { pending.remove(); add('assistant error', error.message); }
    finally { button.disabled = false; input.disabled = false; input.focus(); }
  });
})();
