namespace Marvi
{
    internal static class ExerciseCatalog
    {
        private const string Physics = "Física", Chemistry = "Química";
        private const string Calculus1 = "Cálculo I (Análisis Matemático I)", Calculus2 = "Cálculo II (Análisis Matemático II)";
        private const string Statistics = "Probabilidad y Estadística", Advanced = "Matemática Superior", Numerical = "Análisis Numérico";

        private static MathProblem Problem(string subject, string unit, string question, string solution)
        {
            return new MathProblem(subject, question, solution) { Unit = unit };
        }

        internal static readonly MathProblem[] Examples = {
            Problem(Physics, "Energía y rozamiento", "Un bloque de 2 kg entra a una superficie horizontal con rapidez de 6 m/s. Si μ = 0,20 y g = 10 m/s², ¿qué distancia recorre hasta detenerse?", "La energía cinética inicial es 36 J. El rozamiento vale 4 N. Igualá su trabajo en valor absoluto a la energía inicial: d = 36/4 = 9 m."),
            Problem(Physics, "Circuitos RC", "Un capacitor descargado de 100 μF se conecta a una fuente ideal de 12 V mediante una resistencia de 10 kΩ. Calculá la tensión al cabo de 1 s.", "La constante de tiempo RC es 1 s. Entonces V = 12(1 − e⁻¹) ≈ 7,59 V."),
            Problem(Physics, "Cantidad de movimiento", "Dos carros de 3 y 2 kg chocan y quedan unidos. El primero viaja a 4 m/s y el otro está en reposo. Hallá la velocidad final y la energía cinética perdida.", "Sin impulso externo horizontal, v = 12/5 = 2,4 m/s. La energía baja de 24 J a 14,4 J. Se pierden 9,6 J."),
            Problem(Chemistry, "Equilibrio químico", "En un recipiente de 1 L se introduce 1 mol de N₂O₄. Para N₂O₄ ⇌ 2 NO₂, Kc = 0,20. Calculá las concentraciones de equilibrio.", "Si se disocian x mol/L, 4x²/(1 − x) = 0,20. La raíz física es x = 0,20. Quedan 0,80 mol/L de N₂O₄ y 0,40 mol/L de NO₂."),
            Problem(Chemistry, "Reactivo limitante", "Para 2 H₂ + O₂ → 2 H₂O mezclás 5 mol de H₂ con 2 mol de O₂. Determiná el reactivo limitante, los moles de agua y el exceso remanente.", "2 mol de O₂ consumen 4 mol de H₂. Limita el oxígeno, se forman 4 mol de agua y sobra 1 mol de H₂."),
            Problem(Chemistry, "Cinética de primer orden", "Una reacción de primer orden tiene una semivida de 20 minutos. Si la concentración inicial es 0,80 mol/L, hallá k y la concentración a los 60 minutos.", "k = ln(2)/20 ≈ 0,0347 min⁻¹. En tres semividas queda 0,80/8 = 0,10 mol/L."),
            Problem(Calculus1, "Límites", "Calculá el límite de (1 − cos x)/x² cuando x tiende a cero, con x en radianes.", "Usá 1 − cos x = 2 sen²(x/2). La expresión es (1/2)[sen(x/2)/(x/2)]² y el límite vale 1/2."),
            Problem(Calculus1, "Optimización", "Un rectángulo se apoya contra una pared. Tenés 60 m de cerco para los otros tres lados. ¿Qué dimensiones maximizan el área?", "Si x es la longitud perpendicular a la pared, A(x) = x(60 − 2x). De A′ = 60 − 4x = 0 resulta x = 15 m y el lado paralelo mide 30 m. A″ = −4 confirma el máximo."),
            Problem(Calculus1, "Integración por partes", "Evaluá la integral de x·ln(x) entre 1 y e.", "Tomá u = ln(x) y dv = x dx. Una primitiva es (x²/2)ln(x) − x²/4. Al evaluar los extremos obtenés (e² + 1)/4."),
            Problem(Calculus2, "Derivada direccional", "Para f(x,y) = x²y + y², calculá la derivada en (1,2) en la dirección del vector (3,4).", "El gradiente en (1,2) es (4,5). Normalizá la dirección a (3/5,4/5). El producto escalar vale 32/5."),
            Problem(Calculus2, "Integrales dobles", "Integrá x + y sobre el triángulo x ≥ 0, y ≥ 0, x + y ≤ 1.", "Para x de 0 a 1, integrá primero en y de 0 a 1 − x. Obtenés (1 − x²)/2. Su integral entre 0 y 1 vale 1/3."),
            Problem(Calculus2, "Puntos críticos", "Encontrá y clasificá los puntos críticos de f(x,y) = x² + xy + y² − 3x.", "De 2x + y = 3 y x + 2y = 0 resulta (2,−1). La Hessiana tiene diagonal (2,2) y determinante 3. Es definida positiva en todo el plano, así que el mínimo global vale −3."),
            Problem(Statistics, "Probabilidad condicional", "Una fábrica recibe el 60 % de sus piezas de A y el resto de B. Las tasas de defecto son 2 % y 5 %, respectivamente. Si una pieza es defectuosa, ¿cuál es la probabilidad de que venga de B?", "La probabilidad total de defecto es 0,6·0,02 + 0,4·0,05 = 0,032. Por Bayes, P(B | defecto) = 0,020/0,032 = 62,5 %."),
            Problem(Statistics, "Intervalo de confianza", "Una muestra aleatoria de 36 observaciones normales tiene media 50. El desvío poblacional conocido es 6. Construí un intervalo de confianza del 95 % para la media.", "El error estándar es 6/√36 = 1. Usá el cuantil normal 1,96. El intervalo va de 48,04 a 51,96."),
            Problem(Statistics, "Distribución binomial", "Tres ensayos independientes tienen probabilidad de éxito 0,2 cada uno. Calculá la probabilidad de al menos un éxito y el número esperado de éxitos.", "Por complemento, P(X ≥ 1) = 1 − 0,8³ = 0,488. La esperanza es np = 0,6."),
            Problem(Advanced, "Autovalores", "Hallá los autovalores de la matriz con filas (2,1) y (1,2), y una base de autovectores.", "El polinomio característico es (2 − λ)² − 1. Para λ = 3 sirve (1,1) y para λ = 1 sirve (1,−1). Son independientes."),
            Problem(Advanced, "Raíces complejas", "Determiná las tres raíces cúbicas de −8 en forma polar.", "Escribí −8 = 8·exp(iπ). Las raíces tienen módulo 2 y argumentos π/3, π y 5π/3."),
            Problem(Advanced, "Ecuaciones diferenciales", "Resolvé y′ + 2y = 4, con y(0) = 1.", "Multiplicá por e^(2x). Al integrar, y = 2 + C·e^(−2x). La condición inicial da C = −1."),
            Problem(Numerical, "Método de Newton", "Aproximá √2 aplicando dos iteraciones de Newton a f(x) = x² − 2 desde x₀ = 1.", "La iteración es xₙ₊₁ = (xₙ + 2/xₙ)/2. Resultan x₁ = 1,5 y x₂ = 17/12 ≈ 1,41667."),
            Problem(Numerical, "Error de bisección", "Partís de un intervalo de longitud 2 que encierra una raíz. ¿Cuántas bisecciones garantizan que la longitud restante sea menor o igual que 0,001?", "Necesitás 2/2ⁿ ≤ 0,001, o 2ⁿ ≥ 2000. Como 2¹¹ = 2048, alcanzan 11 bisecciones."),
            Problem(Numerical, "Regla del trapecio", "Aproximá la integral de x² entre 0 y 2 con la regla compuesta del trapecio y dos subintervalos iguales. Compará con el valor exacto.", "Con h = 1, T = (0 + 2·1 + 4)/2 = 3. La integral exacta vale 8/3. Se sobreestima en 1/3.")
        };
    }
}
