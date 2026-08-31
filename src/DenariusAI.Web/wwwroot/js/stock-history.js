(() => {
    const chart = document.querySelector('[data-stock-chart]');
    const dataNode = document.getElementById('stock-chart-data');
    if (!chart || !dataNode) return;

    const data = JSON.parse(dataNode.textContent);
    if (!data.history?.length) return;

    const svg = chart.querySelector('svg');
    const actualLine = chart.querySelector('.stock-chart-line');
    const actualArea = chart.querySelector('.stock-chart-area');
    const forecastLine = chart.querySelector('.stock-forecast-line');
    const forecastBand = chart.querySelector('.stock-forecast-band');
    const forecastPoint = chart.querySelector('.stock-forecast-point');
    const buttons = [...document.querySelectorAll('[data-forecast-days]')];
    const left = 24, right = 876, top = 34, bottom = 236;
    let interactivePoints = [];

    const hoverLine = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    hoverLine.classList.add('stock-chart-hover-line');
    const hoverPoint = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
    hoverPoint.classList.add('stock-chart-hover-point');
    hoverPoint.setAttribute('r', '5');
    svg.append(hoverLine, hoverPoint);

    const tooltip = document.createElement('div');
    tooltip.className = 'stock-chart-tooltip';
    tooltip.setAttribute('role', 'status');
    chart.append(tooltip);

    const formatDate = value => new Intl.DateTimeFormat('pt-PT').format(new Date(`${value}T00:00:00`));
    const formatPrice = value => `${new Intl.NumberFormat('pt-PT', { minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(value)} ${data.currency}`;

    const render = forecast => {
        const firstDate = new Date(`${data.history[0].date}T00:00:00`);
        const lastObserved = data.history[data.history.length - 1];
        const endDate = forecast ? new Date(`${forecast.date}T00:00:00`) : new Date(`${lastObserved.date}T00:00:00`);
        const duration = Math.max(1, endDate - firstDate);
        const values = data.history.map(item => Number(item.price));
        if (forecast) values.push(Number(forecast.lower), Number(forecast.upper), Number(forecast.price));
        let minimum = Math.min(...values), maximum = Math.max(...values);
        const padding = Math.max((maximum - minimum) * .08, maximum * .005, .01);
        minimum -= padding;
        maximum += padding;
        const x = date => left + (new Date(`${date}T00:00:00`) - firstDate) / duration * (right - left);
        const y = value => bottom - (Number(value) - minimum) / (maximum - minimum) * (bottom - top);
        const observed = data.history.map(item => `${x(item.date).toFixed(2)},${y(item.price).toFixed(2)}`);
        actualLine.setAttribute('points', observed.join(' '));
        actualArea.setAttribute('points', `${left},${bottom} ${observed.join(' ')} ${x(lastObserved.date).toFixed(2)},${bottom}`);
        interactivePoints = data.history.map(item => ({ ...item, kind: 'Histórico', x: x(item.date), y: y(item.price) }));

        if (forecast) {
            const origin = `${x(lastObserved.date).toFixed(2)},${y(lastObserved.price).toFixed(2)}`;
            const target = `${x(forecast.date).toFixed(2)},${y(forecast.price).toFixed(2)}`;
            forecastLine.setAttribute('points', `${origin} ${target}`);
            forecastBand.setAttribute('points', `${origin} ${x(forecast.date).toFixed(2)},${y(forecast.upper).toFixed(2)} ${x(forecast.date).toFixed(2)},${y(forecast.lower).toFixed(2)}`);
            forecastPoint.setAttribute('cx', x(forecast.date).toFixed(2));
            forecastPoint.setAttribute('cy', y(forecast.price).toFixed(2));
            interactivePoints.push({ ...forecast, kind: `Previsão a ${forecast.days} dias`, x: x(forecast.date), y: y(forecast.price) });
        }

        buttons.forEach(button => {
            const selected = forecast && Number(button.dataset.forecastDays) === Number(forecast.days);
            button.classList.toggle('active', selected);
            button.setAttribute('aria-pressed', selected.toString());
        });
        tooltip.classList.remove('visible');
        hoverLine.classList.remove('visible');
        hoverPoint.classList.remove('visible');
    };

    svg.addEventListener('pointermove', event => {
        const bounds = svg.getBoundingClientRect();
        const pointerX = (event.clientX - bounds.left) / bounds.width * 900;
        const nearest = interactivePoints.reduce((best, point) => Math.abs(point.x - pointerX) < Math.abs(best.x - pointerX) ? point : best);
        hoverLine.setAttribute('x1', nearest.x);
        hoverLine.setAttribute('x2', nearest.x);
        hoverLine.setAttribute('y1', top);
        hoverLine.setAttribute('y2', bottom);
        hoverPoint.setAttribute('cx', nearest.x);
        hoverPoint.setAttribute('cy', nearest.y);
        hoverLine.classList.add('visible');
        hoverPoint.classList.add('visible');

        const kind = document.createElement('span');
        kind.textContent = nearest.kind;
        const value = document.createElement('strong');
        value.textContent = formatPrice(nearest.price);
        const date = document.createElement('small');
        date.textContent = formatDate(nearest.date);
        tooltip.replaceChildren(kind, value, date);
        tooltip.classList.add('visible');

        const chartBounds = chart.getBoundingClientRect();
        const tooltipX = event.clientX - chartBounds.left;
        const tooltipY = event.clientY - chartBounds.top;
        tooltip.style.left = `${tooltipX}px`;
        tooltip.style.top = `${tooltipY}px`;
        tooltip.classList.toggle('align-left', tooltipX > chartBounds.width * .7);
    });

    svg.addEventListener('pointerleave', () => {
        tooltip.classList.remove('visible');
        hoverLine.classList.remove('visible');
        hoverPoint.classList.remove('visible');
    });

    buttons.forEach(button => button.addEventListener('click', () => render(data.forecasts.find(item => Number(item.days) === Number(button.dataset.forecastDays)))));
    render(data.forecasts?.[0] ?? null);
})();
