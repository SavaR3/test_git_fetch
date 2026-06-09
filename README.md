public async Task DeleteUserWithTransaction(int userId)
{
    // 1. Otwieramy transakcję — usuwanie danych często dotyczy kilku tabel na raz,
    // dlatego musimy mieć pewność, że albo usuniemy wszystko poprawnie, albo nic.
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 2. WALIDACJA: Szukamy użytkownika o podanym ID w bazie danych.
        // Używamy metody .Include(u => u.Deal), aby od razu pobrać z bazy również wszystkie jego umowy.
        var user = await _context.Users
            .Include(u => u.Deal)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            // Jeśli użytkownik nie istnieje, przerywamy proces i rzucamy wyjątek 404.
            throw new NotFoundException($"Użytkownik o ID {userId} nie istnieje.");
        }

        // 3. WALIDACJA BIZNESOWA (Zabezpieczenie przed usunięciem ważnych danych):
        // Sprawdzamy, czy użytkownik ma jakieś "aktywne" umowy (np. takie, które nie mają wpisanej daty zakończenia).
        var hasActiveDeals = user.Deal != null && user.Deal.Any(d => d.DateTo == null || d.DateTo > DateTime.Now);
        
        if (hasActiveDeals)
        {
            // Jeśli użytkownik ma aktywne umowy, aplikacja nie pozwala go usunąć. Tworzymy konflikt.
            throw new ConflictException("Nie można usunąć użytkownika z aktywnymi umowami. Najpierw je zakończ lub usuń.");
        }

        // 4. CZYSZCZENIE POWIĄZAŃ (Usuwanie kaskadowe w kodzie):
        // Jeśli użytkownik ma jakieś stare, historyczne umowy, musimy je najpierw usunąć z tabeli Deal,
        // w przeciwnym wypadku baza danych wyrzuci błąd klucza obcego (Foreign Key Constraint).
        if (user.Deal != null && user.Deal.Any())
        {
            // Usuwamy paczką wszystkie umowy przypisane do tego użytkownika
            _context.Deals.RemoveRange(user.Deal);
        }

        // 5. USUWANIE GŁÓWNEGO OBIEKTU:
        // Po wyczyszczeniu tabeli zależnej (Deal), możemy bezpiecznie nakazać usunięcie samego użytkownika.
        _context.Users.Remove(user);

        // 6. FINISZ: Ef Core generuje odpowiednie komendy SQL "DELETE FROM..." dla umów oraz użytkownika.
        await _context.SaveChangesAsync();
        
        // Zatwierdzamy transakcję — dane zostały trwale usunięte z bazy danych.
        await transaction.CommitAsync();
    }
    catch (Exception ex)
    {
        // W przypadku jakiegokolwiek błędu podczas usuwania, cofamy transakcję.
        // Użytkownik i jego umowy pozostaną nienaruszone w bazie danych.
        await transaction.RollbackAsync();
        throw;
    }
}

=================================================================================================================================================

public async Task CreateDealWithTransaction(PostDealDto dto)
{
    // 1. Otwieramy transakcję — gwarantuje ona, że jeśli baza danych wywali błąd na końcu,
    // żadne niepełne dane nie zostaną zapisane (zasada "wszystko albo nic").
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 2. WALIDACJA: Sprawdzamy, czy użytkownik (UserId), do którego przypisujemy umowę, w ogóle istnieje.
        // Szukamy go w tabeli User na podstawie danych przesłanych przez klienta w JSON (dto).
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
        
        if (user is null)
        {
            // Jeśli użytkownika nie ma w bazie, natychmiast przerywamy i rzucamy wyjątek.
            throw new NotFoundException($"Użytkownik o ID {dto.UserId} nie został znaleziony.");
        }

        // 3. WALIDACJA BIZNESOWA: Sprawdzamy potencjalny konflikt danych.
        // Na przykład: biznes wymaga, aby użytkownik nie miał dwóch umów rozpoczynających się tego samego dnia.
        var hasDuplicateDeal = await _context.Deals
            .AnyAsync(d => d.UserId == dto.UserId && d.DateFrom.Date == dto.DateFrom.Date);
            
        if (hasDuplicateDeal)
        {
            // Jeśli reguła biznesowa została złamana, rzucamy wyjątek konfliktu.
            throw new ConflictException("Ten użytkownik posiada już umowę rozpoczętą w tym dniu.");
        }

        // 4. MAPOWANIE: Przepisujemy dane z obiektu DTO (który przyszedł od klienta)
        // na właściwy obiekt encji Deal, który rozumie Entity Framework i baza danych.
        var newDeal = new Deal
        {
            UserId = dto.UserId,           // Powiązanie umowy z konkretnym użytkownikiem (Klucz obcy)
            Description = dto.Description,
            DateFrom = dto.DateFrom,
            DateTo = dto.DateTo
            // Wartości Id nie ustawiamy ręcznie! Baza danych wygeneruje ją automatycznie (np. Id = 4).
        };

        // 5. REJESTRACJA: Informujemy Entity Framework, że chcemy dodać nowy obiekt do bazy.
        // Na tym etapie obiekt znajduje się tylko w pamięci aplikacji (w tzw. Change Trackerze).
        _context.Deals.Add(newDeal);

        // 6. FINISZ: Zapisujemy zmiany. EF Core generuje zapytanie SQL "INSERT INTO Deal..." i wysyła do bazy.
        await _context.SaveChangesAsync();
        
        // Zamykamy i zatwierdzamy transakcję w bazie danych — operacja zakończona sukcesem.
        await transaction.CommitAsync();
    }
    catch (Exception ex)
    {
        // Jeśli w bloku 'try' cokolwiek poszło nie tak (np. błąd serwera bazy danych),
        // wycofujemy wszystkie dotychczasowe zmiany wprowadzone w tej transakcji.
        await transaction.RollbackAsync();
        throw; // Przekazujemy wyjątek dalej, aby kontroler mógł zwrócić odpowiedni kod błędu (np. 500 lub 400)
    }
}

