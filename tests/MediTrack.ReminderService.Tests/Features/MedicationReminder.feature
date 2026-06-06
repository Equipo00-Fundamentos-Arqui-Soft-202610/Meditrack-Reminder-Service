Feature: Generación de recordatorios de medicación
    Como paciente con un tratamiento activo
    Quiero que se genere un recordatorio cuando se carga mi receta
    Para no olvidar tomar mi medicamento (US05, US13)

    Scenario: Una receta cargada genera un recordatorio de medicación
        Given una receta del paciente 100 con el medicamento "Paracetamol" dosis "500 mg" a las "2026-06-10T08:00:00Z"
        When se procesa el evento RecetaCargada
        Then existe 1 recordatorio programado para el paciente 100
        And el recordatorio queda programado para las "2026-06-10T08:00:00Z"
        And el mensaje del recordatorio menciona "Paracetamol"

    Scenario: Registrar el cumplimiento cancela el recordatorio pendiente
        Given una receta del paciente 200 con el medicamento "Ibuprofeno" dosis "400 mg" a las "2026-06-11T09:00:00Z"
        When se procesa el evento RecetaCargada
        And se procesa el evento CumplimientoRegistrado para el medicamento 1 del paciente 200
        Then el recordatorio del paciente 200 queda cancelado
