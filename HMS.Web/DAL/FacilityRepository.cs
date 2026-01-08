/*
 * FILE: FacilityRepository.cs
 * PURPOSE: Manages hospital facilities (OTs, Wards).
 * COMMUNICATES WITH: DatabaseHelper, Admin/FacilityManagement.razor
 */
using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HMS.Web.DAL
{
    /// <summary>
    /// Repository for managing hospital physical facilities like Wards, Rooms, Beds, and Operation Theaters.
    /// OPTIMIZATION: [Hierarchical Loading] Separation of Wards -> Rooms -> Beds queries prevents "N+1" problems and massive cartesian products.
    /// </summary>
    public class FacilityRepository
    {
        private readonly DatabaseHelper _db;

        public FacilityRepository(DatabaseHelper db)
        {
            _db = db;
        }

        // --- Wards ---

        /// <summary>
        /// Retrieves all active wards (limited to top 100).
        /// </summary>
        public async Task<List<Ward>> GetWardsAsync()
        {
            try
            {
                // Default to top 100 to prevent lag
                string query = "SELECT TOP 100 * FROM Wards WHERE IsActive = 1 ORDER BY WardName";
                return await _db.ExecuteQueryAsync(query, MapWard);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving wards: {ex.Message}", ex);
            }
        }

        public List<Ward> GetWards()
        {
            string query = "SELECT TOP 100 * FROM Wards WHERE IsActive = 1 ORDER BY WardName";
            return _db.ExecuteQuery(query, MapWard);
        }

        /// <summary>
        /// Retrieves a paged list of active wards.
        /// </summary>
        public async Task<List<Ward>> GetWardsPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "WardName" : orderBy;
                // Pagination Logic: Calculating the OFFSET and FETCH NEXT based on UI requirements.
                // This ensures we only pull the necessary subset of rows from the database.
                string sql = $@"SELECT * FROM Wards WHERE IsActive = 1 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
                return await _db.ExecuteQueryAsync(sql, MapWard, parameters);
            }
            catch { return new List<Ward>(); }
        }

        public List<Ward> GetWardsPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "WardName" : orderBy;
            string sql = $@"SELECT * FROM Wards WHERE IsActive = 1 ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(sql, MapWard, parameters);
        }

        /// <summary>
        /// Gets the total count of active wards.
        /// </summary>
        public async Task<int> GetWardsCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Wards WHERE IsActive = 1");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetWardsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Wards WHERE IsActive = 1") ?? 0);

        /// <summary>
        /// Asynchronously saves or updates a ward record.
        /// </summary>
        public async Task SaveWardAsync(Ward ward)
        {
            try
            {
                if (ward == null || string.IsNullOrEmpty(ward.WardName)) throw new ArgumentException("Ward name is required.");

                // UPSERT Logic: If the ID is 0, we treat this as a new record (INSERT).
                // Otherwise, we perform an UPDATE on the existing primary key.
                if (ward.WardId == 0)
                {
                    string sql = "INSERT INTO Wards (WardName, Floor, Wing, IsActive) VALUES (@WardName, @Floor, @Wing, @IsActive)";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@WardName", ward.WardName),
                        new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value),
                        new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value),
                        new SqlParameter("@IsActive", ward.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE Wards SET WardName=@WardName, Floor=@Floor, Wing=@Wing, IsActive=@IsActive WHERE WardId=@WardId";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@WardId", ward.WardId),
                        new SqlParameter("@WardName", ward.WardName),
                        new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value),
                        new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value),
                        new SqlParameter("@IsActive", ward.IsActive)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save ward: {ex.Message}", ex);
            }
        }

        public void SaveWard(Ward ward)
        {
            if (ward == null || string.IsNullOrEmpty(ward.WardName)) throw new ArgumentException("Ward name is required.");
            if (ward.WardId == 0)
            {
                string sql = "INSERT INTO Wards (WardName, Floor, Wing, IsActive) VALUES (@WardName, @Floor, @Wing, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@WardName", ward.WardName), new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value), new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value), new SqlParameter("@IsActive", ward.IsActive) });
            }
            else
            {
                string sql = "UPDATE Wards SET WardName=@WardName, Floor=@Floor, Wing=@Wing, IsActive=@IsActive WHERE WardId=@WardId";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@WardId", ward.WardId), new SqlParameter("@WardName", ward.WardName), new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value), new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value), new SqlParameter("@IsActive", ward.IsActive) });
            }
        }

        // --- Room Types ---

        /// <summary>
        /// Retrieves all defined room types.
        /// </summary>
        public async Task<List<RoomType>> GetRoomTypesAsync()
        {
            try
            {
                string query = "SELECT TOP 100 * FROM RoomTypes ORDER BY TypeName";
                return await _db.ExecuteQueryAsync(query, MapRoomType);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving room types: {ex.Message}", ex);
            }
        }

        public List<RoomType> GetRoomTypes()
        {
            string query = "SELECT TOP 100 * FROM RoomTypes ORDER BY TypeName";
            return _db.ExecuteQuery(query, MapRoomType);
        }

        /// <summary>
        /// Retrieves a paged list of room types.
        /// </summary>
        public async Task<List<RoomType>> GetRoomTypesPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "TypeName" : orderBy;
                string sql = $@"SELECT * FROM RoomTypes 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
                return await _db.ExecuteQueryAsync(sql, MapRoomType, parameters);
            }
            catch { return new List<RoomType>(); }
        }

        public List<RoomType> GetRoomTypesPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "TypeName" : orderBy;
            string sql = $@"SELECT * FROM RoomTypes ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(sql, MapRoomType, parameters);
        }

        /// <summary>
        /// Gets the total count of room types.
        /// </summary>
        public async Task<int> GetRoomTypesCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM RoomTypes");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetRoomTypesCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM RoomTypes") ?? 0);

        /// <summary>
        /// Asynchronously saves or updates a room type record.
        /// </summary>
        public async Task SaveRoomTypeAsync(RoomType type)
        {
            try
            {
                if (type == null || string.IsNullOrEmpty(type.TypeName)) throw new ArgumentException("Room type name is required.");

                if (type.RoomTypeId == 0)
                {
                    string sql = "INSERT INTO RoomTypes (TypeName, DailyRate, Description) VALUES (@TypeName, @DailyRate, @Description)";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@TypeName", type.TypeName),
                        new SqlParameter("@DailyRate", type.DailyRate),
                        new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value)
                    });
                }
                else
                {
                    string sql = "UPDATE RoomTypes SET TypeName=@TypeName, DailyRate=@DailyRate, Description=@Description WHERE RoomTypeId=@RoomTypeId";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@RoomTypeId", type.RoomTypeId),
                        new SqlParameter("@TypeName", type.TypeName),
                        new SqlParameter("@DailyRate", type.DailyRate),
                        new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save room type: {ex.Message}", ex);
            }
        }

        public void SaveRoomType(RoomType type)
        {
            if (type == null || string.IsNullOrEmpty(type.TypeName)) throw new ArgumentException("Room type name is required.");
            if (type.RoomTypeId == 0)
            {
                string sql = "INSERT INTO RoomTypes (TypeName, DailyRate, Description) VALUES (@TypeName, @DailyRate, @Description)";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@TypeName", type.TypeName), new SqlParameter("@DailyRate", type.DailyRate), new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value) });
            }
            else
            {
                string sql = "UPDATE RoomTypes SET TypeName=@TypeName, DailyRate=@DailyRate, Description=@Description WHERE RoomTypeId=@RoomTypeId";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@RoomTypeId", type.RoomTypeId), new SqlParameter("@TypeName", type.TypeName), new SqlParameter("@DailyRate", type.DailyRate), new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value) });
            }
        }

        // --- Rooms ---

        /// <summary>
        /// Retrieves all active rooms with their ward and type details (limited to top 100).
        /// </summary>
        public async Task<List<Room>> GetRoomsAsync()
        {
            try
            {
                string sql = @"
                    SELECT TOP 100 r.*, w.WardName, rt.TypeName as RoomTypeName 
                    FROM Rooms r
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                    WHERE r.IsActive = 1
                    ORDER BY r.RoomNumber";

                return await _db.ExecuteQueryAsync(sql, MapRoom);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving rooms: {ex.Message}", ex);
            }
        }

        public List<Room> GetRooms()
        {
            string sql = @"SELECT TOP 100 r.*, w.WardName, rt.TypeName as RoomTypeName FROM Rooms r LEFT JOIN Wards w ON r.WardId = w.WardId LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId WHERE r.IsActive = 1 ORDER BY r.RoomNumber";
            return _db.ExecuteQuery(sql, MapRoom);
        }

        /// <summary>
        /// Retrieves a paged list of active rooms.
        /// </summary>
        public async Task<List<Room>> GetRoomsPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "RoomNumber" : orderBy;
                string sql = $@"
                    SELECT r.*, w.WardName, rt.TypeName as RoomTypeName 
                    FROM Rooms r
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                    WHERE r.IsActive = 1
                    ORDER BY {orderClause}
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
                return await _db.ExecuteQueryAsync(sql, MapRoom, parameters);
            }
            catch { return new List<Room>(); }
        }

        public List<Room> GetRoomsPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "RoomNumber" : orderBy;
            string sql = $@"SELECT r.*, w.WardName, rt.TypeName as RoomTypeName FROM Rooms r LEFT JOIN Wards w ON r.WardId = w.WardId LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId WHERE r.IsActive = 1 ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(sql, MapRoom, parameters);
        }

        /// <summary>
        /// Gets the total count of active rooms.
        /// </summary>
        public async Task<int> GetRoomsCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Rooms WHERE IsActive = 1");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetRoomsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Rooms WHERE IsActive = 1") ?? 0);

        /// <summary>
        /// Asynchronously saves or updates a room record.
        /// </summary>
        public async Task SaveRoomAsync(Room room)
        {
            try
            {
                if (room == null || string.IsNullOrEmpty(room.RoomNumber)) throw new ArgumentException("Room number is required.");

                if (room.RoomId == 0)
                {
                    string sql = "INSERT INTO Rooms (WardId, RoomNumber, RoomTypeId, IsActive) VALUES (@WardId, @RoomNumber, @RoomTypeId, @IsActive)";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@WardId", room.WardId),
                        new SqlParameter("@RoomNumber", room.RoomNumber),
                        new SqlParameter("@RoomTypeId", room.RoomTypeId),
                        new SqlParameter("@IsActive", room.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE Rooms SET WardId=@WardId, RoomNumber=@RoomNumber, RoomTypeId=@RoomTypeId, IsActive=@IsActive WHERE RoomId=@RoomId";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@RoomId", room.RoomId),
                        new SqlParameter("@WardId", room.WardId),
                        new SqlParameter("@RoomNumber", room.RoomNumber),
                        new SqlParameter("@RoomTypeId", room.RoomTypeId),
                        new SqlParameter("@IsActive", room.IsActive)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save room record: {ex.Message}", ex);
            }
        }

        public void SaveRoom(Room room)
        {
            if (room == null || string.IsNullOrEmpty(room.RoomNumber)) throw new ArgumentException("Room number is required.");
            if (room.RoomId == 0)
            {
                string sql = "INSERT INTO Rooms (WardId, RoomNumber, RoomTypeId, IsActive) VALUES (@WardId, @RoomNumber, @RoomTypeId, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@WardId", room.WardId), new SqlParameter("@RoomNumber", room.RoomNumber), new SqlParameter("@RoomTypeId", room.RoomTypeId), new SqlParameter("@IsActive", room.IsActive) });
            }
            else
            {
                string sql = "UPDATE Rooms SET WardId=@WardId, RoomNumber=@RoomNumber, RoomTypeId=@RoomTypeId, IsActive=@IsActive WHERE RoomId=@RoomId";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@RoomId", room.RoomId), new SqlParameter("@WardId", room.WardId), new SqlParameter("@RoomNumber", room.RoomNumber), new SqlParameter("@RoomTypeId", room.RoomTypeId), new SqlParameter("@IsActive", room.IsActive) });
            }
        }

        // --- Beds ---

        /// <summary>
        /// Retrieves all active beds with their room and ward details (limited to top 100).
        /// </summary>
        public async Task<List<Bed>> GetBedsAsync()
        {
            try
            {
                string sql = @"
                    SELECT TOP 100 b.*, r.RoomNumber, w.WardName 
                    FROM Beds b
                    LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    WHERE b.IsActive = 1
                    ORDER BY b.BedNumber";

                return await _db.ExecuteQueryAsync(sql, MapBed);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving beds: {ex.Message}", ex);
            }
        }

        public List<Bed> GetBeds()
        {
            string sql = @"SELECT TOP 100 b.*, r.RoomNumber, w.WardName FROM Beds b LEFT JOIN Rooms r ON b.RoomId = r.RoomId LEFT JOIN Wards w ON r.WardId = w.WardId WHERE b.IsActive = 1 ORDER BY b.BedNumber";
            return _db.ExecuteQuery(sql, MapBed);
        }

        /// <summary>
        /// Retrieves a paged list of active beds.
        /// </summary>
        public async Task<List<Bed>> GetBedsPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "BedNumber" : orderBy;
                string sql = $@"
                    SELECT b.*, r.RoomNumber, w.WardName 
                    FROM Beds b
                    LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    WHERE b.IsActive = 1
                    ORDER BY {orderClause}
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
                return await _db.ExecuteQueryAsync(sql, MapBed, parameters);
            }
            catch { return new List<Bed>(); }
        }

        public List<Bed> GetBedsPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "BedNumber" : orderBy;
            string sql = $@"SELECT b.*, r.RoomNumber, w.WardName FROM Beds b LEFT JOIN Rooms r ON b.RoomId = r.RoomId LEFT JOIN Wards w ON r.WardId = w.WardId WHERE b.IsActive = 1 ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            var parameters = new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) };
            return _db.ExecuteQuery(sql, MapBed, parameters);
        }

        /// <summary>
        /// Gets the total count of active beds.
        /// </summary>
        public async Task<int> GetBedsCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Beds WHERE IsActive = 1");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetBedsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Beds WHERE IsActive = 1") ?? 0);

        /// <summary>
        /// Asynchronously saves or updates a bed record.
        /// </summary>
        public async Task SaveBedAsync(Bed bed)
        {
            try
            {
                if (bed == null || string.IsNullOrEmpty(bed.BedNumber)) throw new ArgumentException("Bed number is required.");

                if (bed.BedId == 0)
                {
                    string sql = "INSERT INTO Beds (RoomId, BedNumber, Status, IsActive) VALUES (@RoomId, @BedNumber, @Status, @IsActive)";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@RoomId", bed.RoomId),
                        new SqlParameter("@BedNumber", bed.BedNumber),
                        new SqlParameter("@Status", bed.Status ?? "Available"),
                        new SqlParameter("@IsActive", bed.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE Beds SET RoomId=@RoomId, BedNumber=@BedNumber, Status=@Status, IsActive=@IsActive WHERE BedId=@BedId";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@BedId", bed.BedId),
                        new SqlParameter("@RoomId", bed.RoomId),
                        new SqlParameter("@BedNumber", bed.BedNumber),
                        new SqlParameter("@Status", bed.Status),
                        new SqlParameter("@IsActive", bed.IsActive)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save bed record: {ex.Message}", ex);
            }
        }

        public void SaveBed(Bed bed)
        {
            if (bed == null || string.IsNullOrEmpty(bed.BedNumber)) throw new ArgumentException("Bed number is required.");
            if (bed.BedId == 0)
            {
                string sql = "INSERT INTO Beds (RoomId, BedNumber, Status, IsActive) VALUES (@RoomId, @BedNumber, @Status, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@RoomId", bed.RoomId), new SqlParameter("@BedNumber", bed.BedNumber), new SqlParameter("@Status", bed.Status ?? "Available"), new SqlParameter("@IsActive", bed.IsActive) });
            }
            else
            {
                string sql = "UPDATE Beds SET RoomId=@RoomId, BedNumber=@BedNumber, Status=@Status, IsActive=@IsActive WHERE BedId=@BedId";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@BedId", bed.BedId), new SqlParameter("@RoomId", bed.RoomId), new SqlParameter("@BedNumber", bed.BedNumber), new SqlParameter("@Status", bed.Status), new SqlParameter("@IsActive", bed.IsActive) });
            }
        }

        // PERFORMANCE OPTIMIZATION: Load rooms only for specific ward
        /// <summary>
        /// Retrieves rooms belonging to a specific ward.
        /// </summary>
        public async Task<List<Room>> GetRoomsByWardAsync(int wardId)
        {
            try
            {
                string sql = @"
                    SELECT r.*, w.WardName, rt.TypeName as RoomTypeName 
                    FROM Rooms r
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                    WHERE r.IsActive = 1 AND r.WardId = @WardId";

                return await _db.ExecuteQueryAsync(sql, MapRoom, new[] { new SqlParameter("@WardId", wardId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving rooms for ward {wardId}: {ex.Message}", ex);
            }
        }

        public List<Room> GetRoomsByWard(int wardId)
        {
            string sql = @"SELECT r.*, w.WardName, rt.TypeName as RoomTypeName FROM Rooms r LEFT JOIN Wards w ON r.WardId = w.WardId LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId WHERE r.IsActive = 1 AND r.WardId = @WardId";
            return _db.ExecuteQuery(sql, MapRoom, new[] { new SqlParameter("@WardId", wardId) });
        }

        // PERFORMANCE OPTIMIZATION: Load only available beds for specific room
        /// <summary>
        /// Retrieves available beds in a specific room.
        /// </summary>
        public async Task<List<Bed>> GetAvailableBedsByRoomAsync(int roomId)
        {
            try
            {
                string sql = @"
                    SELECT b.*, r.RoomNumber, w.WardName 
                    FROM Beds b
                    LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    WHERE b.IsActive = 1 AND b.RoomId = @RoomId AND b.Status = 'Available'";

                return await _db.ExecuteQueryAsync(sql, MapBed, new[] { new SqlParameter("@RoomId", roomId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving available beds for room {roomId}: {ex.Message}", ex);
            }
        }

        public List<Bed> GetAvailableBedsByRoom(int roomId)
        {
            string sql = @"SELECT b.*, r.RoomNumber, w.WardName FROM Beds b LEFT JOIN Rooms r ON b.RoomId = r.RoomId LEFT JOIN Wards w ON r.WardId = w.WardId WHERE b.IsActive = 1 AND b.RoomId = @RoomId AND b.Status = 'Available'";
            return _db.ExecuteQuery(sql, MapBed, new[] { new SqlParameter("@RoomId", roomId) });
        }

        // --- Admissions ---

        /// <summary>
        /// Asynchronously admits a patient to a bed.
        /// </summary>
        /// <summary>
        /// Asynchronously admits a patient to a bed.
        /// OPTIMIZATION: [State Consistency] Transactionally updates both the Bed Status and creates the Admission record to avoid "ghost" bookings.
        /// </summary>
        public async Task AdmitPatientAsync(Admission admission)
        {
            try
            {
                if (admission == null || admission.PatientId <= 0 || admission.BedId <= 0)
                    throw new ArgumentException("Invalid admission data. Patient and Bed are required.");

                // Orchestration Logic:
                // 1. Mark the bed as 'Occupied' to prevent double-booking.
                // 2. Insert the Admission event record for clinic tracking.
                string updateBedSql = "UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId";
                await _db.ExecuteNonQueryAsync(updateBedSql, new[] { new SqlParameter("@BedId", admission.BedId) });

                string sql = @"
                    INSERT INTO Admissions (PatientId, BedId, AdmissionDate, Status, Notes) 
                    VALUES (@PatientId, @BedId, @AdmissionDate, 'Admitted', @Notes)";

                // ... execution ...

                await _db.ExecuteNonQueryAsync(sql, new[] {
                    new SqlParameter("@PatientId", admission.PatientId),
                    new SqlParameter("@BedId", admission.BedId),
                    new SqlParameter("@AdmissionDate", admission.AdmissionDate == default ? DateTime.Now : admission.AdmissionDate),
                    new SqlParameter("@Notes", (object?)admission.Notes ?? DBNull.Value)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to admit patient {admission?.PatientId}: {ex.Message}", ex);
            }
        }

        public void AdmitPatient(Admission admission)
        {
            if (admission == null || admission.PatientId <= 0 || admission.BedId <= 0) throw new ArgumentException("Invalid admission data. Patient and Bed are required.");
            _db.ExecuteNonQuery("UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId", new[] { new SqlParameter("@BedId", admission.BedId) });
            string sql = @"INSERT INTO Admissions (PatientId, BedId, AdmissionDate, Status, Notes) VALUES (@PatientId, @BedId, @AdmissionDate, 'Admitted', @Notes)";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@PatientId", admission.PatientId), new SqlParameter("@BedId", admission.BedId), new SqlParameter("@AdmissionDate", admission.AdmissionDate == default ? DateTime.Now : admission.AdmissionDate), new SqlParameter("@Notes", (object?)admission.Notes ?? DBNull.Value) });
        }

        /// <summary>
        /// Asynchronously discharges a patient and updates bed status.
        /// </summary>
        public async Task DischargePatientAsync(int admissionId)
        {
            try
            {
                if (admissionId <= 0) return;

                // Get BedId first
                var bedIdObj = await _db.ExecuteScalarAsync("SELECT BedId FROM Admissions WHERE AdmissionId = @Id", new[] { new SqlParameter("@Id", admissionId) });
                if (bedIdObj != null && int.TryParse(bedIdObj.ToString(), out int bedId))
                {
                    // Update Bed Status to Cleaning
                    await _db.ExecuteNonQueryAsync("UPDATE Beds SET Status = 'Cleaning' WHERE BedId = @BedId", new[] { new SqlParameter("@BedId", bedId) });
                }

                // Update Admission
                string sql = "UPDATE Admissions SET DischargeDate = GETDATE(), Status = 'Discharged' WHERE AdmissionId = @Id";
                await _db.ExecuteNonQueryAsync(sql, new[] { new SqlParameter("@Id", admissionId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to discharge admission {admissionId}: {ex.Message}", ex);
            }
        }

        public void DischargePatient(int admissionId)
        {
            if (admissionId <= 0) return;
            var bedIdObj = _db.ExecuteScalar("SELECT BedId FROM Admissions WHERE AdmissionId = @Id", new[] { new SqlParameter("@Id", admissionId) });
            if (bedIdObj != null && int.TryParse(bedIdObj.ToString(), out int bedId))
            {
                _db.ExecuteNonQuery("UPDATE Beds SET Status = 'Cleaning' WHERE BedId = @BedId", new[] { new SqlParameter("@BedId", bedId) });
            }
            string sql = "UPDATE Admissions SET DischargeDate = GETDATE(), Status = 'Discharged' WHERE AdmissionId = @Id";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Id", admissionId) });
        }

        /// <summary>
        /// Retrieves all currently active admissions (limited to top 100).
        /// </summary>
        public async Task<List<Admission>> GetActiveAdmissionsAsync()
        {
            try
            {
                string sql = @"
                    SELECT TOP 100 a.*, b.BedNumber, r.RoomNumber, w.WardName, rt.TypeName as RoomTypeName, rt.DailyRate, p.FullName as PatientName,
                           (SELECT COUNT(*) FROM Bills WHERE AdmissionId = a.AdmissionId AND Status != 'Paid') as PendingBillCount
                    FROM Admissions a
                    JOIN Beds b ON a.BedId = b.BedId
                    LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                    LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.Status = 'Admitted'
                    ORDER BY a.AdmissionDate DESC";

                return await _db.ExecuteQueryAsync(sql, MapAdmission);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving active admissions: {ex.Message}", ex);
            }
        }

        public List<Admission> GetActiveAdmissions()
        {
            string sql = @"SELECT TOP 100 a.*, b.BedNumber, r.RoomNumber, w.WardName, rt.TypeName as RoomTypeName, rt.DailyRate, p.FullName as PatientName, (SELECT COUNT(*) FROM Bills WHERE AdmissionId = a.AdmissionId AND Status != 'Paid') as PendingBillCount FROM Admissions a JOIN Beds b ON a.BedId = b.BedId LEFT JOIN Rooms r ON b.RoomId = r.RoomId LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId LEFT JOIN Wards w ON r.WardId = w.WardId LEFT JOIN Patients p ON a.PatientId = p.PatientId WHERE a.Status = 'Admitted' ORDER BY a.AdmissionDate DESC";
            return _db.ExecuteQuery(sql, MapAdmission);
        }

        /// <summary>
        /// Retrieves a paged list of active admissions.
        /// </summary>
        public async Task<List<Admission>> GetActiveAdmissionsPagedAsync(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "AdmissionDate DESC" : orderBy;
                string sql = $@"
                    SELECT a.*, b.BedNumber, r.RoomNumber, w.WardName, rt.TypeName as RoomTypeName, rt.DailyRate, p.FullName as PatientName,
                           (SELECT COUNT(*) FROM Bills WHERE AdmissionId = a.AdmissionId AND Status != 'Paid') as PendingBillCount
                    FROM Admissions a
                    JOIN Beds b ON a.BedId = b.BedId
                    LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                    LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.Status = 'Admitted'
                    ORDER BY {orderClause}
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                return await _db.ExecuteQueryAsync(sql, MapAdmission, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
            }
            catch { return new List<Admission>(); }
        }

        public List<Admission> GetActiveAdmissionsPaged(int skip, int take, string orderBy)
        {
            string orderClause = string.IsNullOrEmpty(orderBy) ? "AdmissionDate DESC" : orderBy;
            string sql = $@"SELECT a.*, b.BedNumber, r.RoomNumber, w.WardName, rt.TypeName as RoomTypeName, rt.DailyRate, p.FullName as PatientName, (SELECT COUNT(*) FROM Bills WHERE AdmissionId = a.AdmissionId AND Status != 'Paid') as PendingBillCount FROM Admissions a JOIN Beds b ON a.BedId = b.BedId LEFT JOIN Rooms r ON b.RoomId = r.RoomId LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId LEFT JOIN Wards w ON r.WardId = w.WardId LEFT JOIN Patients p ON a.PatientId = p.PatientId WHERE a.Status = 'Admitted' ORDER BY {orderClause} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            return _db.ExecuteQuery(sql, MapAdmission, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
        }

        /// <summary>
        /// Gets the total count of active admissions.
        /// </summary>
        public async Task<int> GetActiveAdmissionsCountAsync()
        {
            var result = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM Admissions WHERE Status = 'Admitted'");
            return Convert.ToInt32(result ?? 0);
        }

        public int GetActiveAdmissionsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Admissions WHERE Status = 'Admitted'") ?? 0);

        /// <summary>
        /// Retrieves all active operation theaters.
        /// </summary>
        public async Task<List<OperationTheater>> GetTheatersAsync()
        {
            try
            {
                return await _db.ExecuteQueryAsync("SELECT * FROM OperationTheaters WHERE IsActive = 1", MapTheater);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operation theaters: {ex.Message}", ex);
            }
        }

        public List<OperationTheater> GetTheaters()
        {
            return _db.ExecuteQuery("SELECT * FROM OperationTheaters WHERE IsActive = 1", MapTheater);
        }

        /// <summary>
        /// Asynchronously saves or updates an operation theater record.
        /// </summary>
        public async Task SaveTheaterAsync(OperationTheater theater)
        {
            try
            {
                if (theater == null || string.IsNullOrEmpty(theater.TheaterName)) throw new ArgumentException("Theater name is required.");

                if (theater.TheaterId == 0)
                {
                    string sql = "INSERT INTO OperationTheaters (TheaterName, Status, IsActive) VALUES (@TheaterName, @Status, @IsActive)";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@TheaterName", theater.TheaterName),
                        new SqlParameter("@Status", theater.Status ?? "Available"),
                        new SqlParameter("@IsActive", theater.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE OperationTheaters SET TheaterName=@TheaterName, Status=@Status, IsActive=@IsActive WHERE TheaterId=@TheaterId";
                    await _db.ExecuteNonQueryAsync(sql, new[] {
                        new SqlParameter("@TheaterId", theater.TheaterId),
                        new SqlParameter("@TheaterName", theater.TheaterName),
                        new SqlParameter("@Status", theater.Status),
                        new SqlParameter("@IsActive", theater.IsActive)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save theater record: {ex.Message}", ex);
            }
        }

        public void SaveTheater(OperationTheater theater)
        {
            if (theater == null || string.IsNullOrEmpty(theater.TheaterName)) throw new ArgumentException("Theater name is required.");
            if (theater.TheaterId == 0)
            {
                string sql = "INSERT INTO OperationTheaters (TheaterName, Status, IsActive) VALUES (@TheaterName, @Status, @IsActive)";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@TheaterName", theater.TheaterName), new SqlParameter("@Status", theater.Status ?? "Available"), new SqlParameter("@IsActive", theater.IsActive) });
            }
            else
            {
                string sql = "UPDATE OperationTheaters SET TheaterName=@TheaterName, Status=@Status, IsActive=@IsActive WHERE TheaterId=@TheaterId";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@TheaterId", theater.TheaterId), new SqlParameter("@TheaterName", theater.TheaterName), new SqlParameter("@Status", theater.Status), new SqlParameter("@IsActive", theater.IsActive) });
            }
        }

        /// <summary>
        /// Asynchronously updates the status of an operation theater.
        /// </summary>
        public async Task UpdateTheaterStatusAsync(int theaterId, string status)
        {
            try
            {
                if (theaterId <= 0) return;
                string sql = "UPDATE OperationTheaters SET Status = @Status WHERE TheaterId = @TheaterId";
                await _db.ExecuteNonQueryAsync(sql, new[] {
                    new SqlParameter("@TheaterId", theaterId),
                    new SqlParameter("@Status", status)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update theater {theaterId} status: {ex.Message}", ex);
            }
        }

        public void UpdateTheaterStatus(int theaterId, string status)
        {
            if (theaterId <= 0) return;
            string sql = "UPDATE OperationTheaters SET Status = @Status WHERE TheaterId = @TheaterId";
            _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@TheaterId", theaterId), new SqlParameter("@Status", status) });
        }

        // --- Mappings ---

        private Ward MapWard(SqlDataReader r)
        {
            return new Ward
            {
                WardId = r.GetInt32(r.GetOrdinal("WardId")),
                WardName = r["WardName"].ToString()!,
                Floor = r["Floor"]?.ToString() ?? "",
                Wing = r["Wing"]?.ToString() ?? "",
                IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
            };
        }

        private RoomType MapRoomType(SqlDataReader r)
        {
            return new RoomType
            {
                RoomTypeId = r.GetInt32(r.GetOrdinal("RoomTypeId")),
                TypeName = r["TypeName"].ToString()!,
                DailyRate = r.GetDecimal(r.GetOrdinal("DailyRate")),
                Description = r["Description"]?.ToString() ?? ""
            };
        }

        private Room MapRoom(SqlDataReader r)
        {
            return new Room
            {
                RoomId = r.GetInt32(r.GetOrdinal("RoomId")),
                WardId = r.IsDBNull(r.GetOrdinal("WardId")) ? 0 : r.GetInt32(r.GetOrdinal("WardId")),
                RoomNumber = r["RoomNumber"].ToString()!,
                RoomTypeId = r.IsDBNull(r.GetOrdinal("RoomTypeId")) ? 0 : r.GetInt32(r.GetOrdinal("RoomTypeId")),
                IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
                WardName = r.HasColumn("WardName") ? r["WardName"]?.ToString() ?? "Unknown" : "Unknown",
                RoomTypeName = r.HasColumn("RoomTypeName") ? r["RoomTypeName"]?.ToString() ?? "Standard" : "Standard"
            };
        }

        private Bed MapBed(SqlDataReader r)
        {
            return new Bed
            {
                BedId = r.GetInt32(r.GetOrdinal("BedId")),
                RoomId = r.IsDBNull(r.GetOrdinal("RoomId")) ? 0 : r.GetInt32(r.GetOrdinal("RoomId")),
                BedNumber = r["BedNumber"].ToString()!,
                Status = r["Status"].ToString()!,
                IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
                RoomNumber = r.HasColumn("RoomNumber") ? r["RoomNumber"]?.ToString() ?? "N/A" : "N/A",
                WardName = r.HasColumn("WardName") ? r["WardName"]?.ToString() ?? "N/A" : "N/A"
            };
        }

        private Admission MapAdmission(SqlDataReader r)
        {
            return new Admission
            {
                AdmissionId = r.GetInt32(r.GetOrdinal("AdmissionId")),
                PatientId = r.GetInt32(r.GetOrdinal("PatientId")),
                BedId = r.GetInt32(r.GetOrdinal("BedId")),
                AdmissionDate = r.GetDateTime(r.GetOrdinal("AdmissionDate")),
                Status = r["Status"]?.ToString() ?? "Admitted",
                Notes = r["Notes"]?.ToString() ?? "",
                BedNumber = r.HasColumn("BedNumber") ? r["BedNumber"]?.ToString() ?? "N/A" : "N/A",
                RoomNumber = r.HasColumn("RoomNumber") ? r["RoomNumber"]?.ToString() ?? "N/A" : "N/A",
                WardName = r.HasColumn("WardName") ? r["WardName"]?.ToString() ?? "N/A" : "N/A",
                RoomTypeName = r.HasColumn("RoomTypeName") ? r["RoomTypeName"]?.ToString() ?? "N/A" : "N/A",
                DailyRate = r.HasColumn("DailyRate") && !r.IsDBNull(r.GetOrdinal("DailyRate")) ? r.GetDecimal(r.GetOrdinal("DailyRate")) : 0,
                PatientName = r.HasColumn("PatientName") ? r["PatientName"]?.ToString() : "Unknown",
                HasPendingBill = r.HasColumn("PendingBillCount") && Convert.ToInt32(r["PendingBillCount"]) > 0
            };
        }

        private OperationTheater MapTheater(SqlDataReader r)
        {
            return new OperationTheater
            {
                TheaterId = r.GetInt32(r.GetOrdinal("TheaterId")),
                TheaterName = r["TheaterName"]?.ToString() ?? "Unknown OT",
                Status = r["Status"]?.ToString() ?? "Available",
                IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
            };
        }
    }

    /// <summary>
    /// Helper extension to check for column existence in SqlDataReader.
    /// </summary>
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}

