(() => {
    const chart = document.querySelector('[data-savings-rate-chart]');
    const dataNode = document.getElementById('savings-rate-chart-data');
    if (!chart || !dataNode) return;

    const data = JSON.parse(dataNode.textContent);
    if (!data.history?.length) return;

    const svg = chart.querySelector('svg');
    const actualLine = chart.querySelector('.stock-chart-line');
    const actualArea = chart.querySelector('.stock-chart-area');
    const forecastLine = chart.querySelector('.stock-forecast-line');
    const forecastBand = chart.querySelector('.stock-forecast-band');
    const forecastPoint = chart.querySelector('.stock-forecast-point');
    const left = 24, right = 876, top = 34, bottom = 236;

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

    const formatDate = value => new Intl.DateTimeFormat('pt-PT', { month: 'long', year: 'numeric' }).format(new Date(`${value}T00:00:00`));
    const formatRate = value => `${new Intl.NumberFormat('pt-PT', { minimumFractionDigits: 3, maximumFractionDigits: 3 }).format(value)}%`;

    const firstDate = new Date(`${data.history[0].date}T00:00:00`);
    const lastObserved = data.history[data.history.length - 1];
    const endDate = data.forecast ? new Date(`${data.forecast.date}T00:00:00`) : new Date(`${lastObserved.date}T00:00:00`);
    const duration = Math.max(1, endDate - firstDate);
    const values = data.history.map(item => Number(item.rate));
    if (data.forecast) values.push(Number(data.forecast.lower), Number(data.forecast.upper), Number(data.forecast.rate));
    let minimum = Math.min(...values), maximum = Math.max(...values);
    const padding = Math.max((maximum - minimum) * .1, .025);
    minimum = Math.max(0, minimum - padding);
    maximum += padding;
    if (maximum <= minimum) maximum = minimum + .1;

    const x = date => left + (new Date(`${date}T00:00:00`) - firstDate) / duration * (right - left);
    const y = value => bottom - (Number(value) - minimum) / (maximum - minimum) * (bottom - top);
    const observed = data.history.map(item => `${x(item.date).toFixed(2)},${y(item.rate).toFixed(2)}`);
    actualLine.setAttribute('points', observed.join(' '));
    actualArea.setAttribute('points', `${left},${bottom} ${observed.join(' ')} ${x(lastObserved.date).toFixed(2)},${bottom}`);

    const interactivePoints = data.history.map(item => ({ ...item, kind: 'Taxa oficial', x: x(item.date), y: y(item.rate) }));
    if (data.forecast) {
        const origin = `${x(lastObserved.date).toFixed(2)},${y(lastObserved.rate).toFixed(2)}`;
        const target = `${x(data.forecast.date).toFixed(2)},${y(data.forecast.rate).toFixed(2)}`;
        forecastLine.setAttribute('points', `${origin} ${target}`);
        forecastBand.setAttribute('points', `${origin} ${x(data.forecast.date).toFixed(2)},${y(data.forecast.upper).toFixed(2)} ${x(data.forecast.date).toFixed(2)},${y(data.forecast.lower).toFixed(2)}`);
        forecastPoint.setAttribute('cx', x(data.forecast.date).toFixed(2));
        forecastPoint.setAttribute('cy', y(data.forecast.rate).toFixed(2));
        interactivePoints.push({ ...data.forecast, kind: 'Previsão ARIMA', x: x(data.forecast.date), y: y(data.forecast.rate) });
    }

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
        value.textContent = formatRate(nearest.rate);
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
})();
