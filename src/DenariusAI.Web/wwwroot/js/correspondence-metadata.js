(() => {
  const rows = document.querySelector('[data-metadata-rows]');
  const add = document.querySelector('[data-add-metadata]');
  if (!rows || !add) return;
  add.addEventListener('click', () => {
    const count = rows.querySelectorAll('[data-metadata-row]').length;
    if (count >= 30) return;
    rows.querySelector('[data-metadata-empty]')?.remove();
    const row = document.createElement('div'); row.className = 'metadata-row'; row.dataset.metadataRow = '';
    row.innerHTML = `<input type="hidden" name="Items[${count}].Id" value="00000000-0000-0000-0000-000000000000"><div><label for="Items_${count}__Key">Chave</label><input id="Items_${count}__Key" name="Items[${count}].Key" maxlength="120"></div><div><label for="Items_${count}__Value">Valor</label><textarea id="Items_${count}__Value" name="Items[${count}].Value" maxlength="1000" rows="2"></textarea></div><div class="metadata-row-state"><label class="metadata-remove"><input type="checkbox" name="Items[${count}].Remove" value="true"> Remover</label></div>`;
    rows.appendChild(row); row.querySelector('input[name$=".Key"]')?.focus();
  });
})();
