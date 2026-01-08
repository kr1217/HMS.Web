using HMS.Web.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMS.Web.DAL
{
    public class FacilityRepository
    {
        private readonly DatabaseHelper _db;

        public FacilityRepository(DatabaseHelper db)
        {
            _db = db;
        }

        // --- Wards ---
        public List<Ward> GetWards()
        {
            try
            {
                // Default to top 100 to prevent lag
                return _db.ExecuteQuery("SELECT TOP 100 * FROM Wards WHERE IsActive = 1 ORDER BY WardName", r => new Ward
                {
                    WardId = (int)r["WardId"],
                    WardName = r["WardName"].ToString()!,
                    Floor = r["Floor"]?.ToString() ?? "",
                    Wing = r["Wing"]?.ToString() ?? "",
                    IsActive = (bool)r["IsActive"]
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving wards: {ex.Message}", ex);
            }
        }

        public List<Ward> GetWardsPaged(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "WardName" : orderBy;
                string sql = $@"SELECT * FROM Wards WHERE IsActive = 1 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                return _db.ExecuteQuery(sql, r => new Ward
                {
                    WardId = (int)r["WardId"],
                    WardName = r["WardName"].ToString()!,
                    Floor = r["Floor"]?.ToString() ?? "",
                    Wing = r["Wing"]?.ToString() ?? "",
                    IsActive = (bool)r["IsActive"]
                }, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
            }
            catch { return new List<Ward>(); }
        }

        public int GetWardsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Wards WHERE IsActive = 1") ?? 0);

        public void SaveWard(Ward ward)
        {
            try
            {
                if (ward == null || string.IsNullOrEmpty(ward.WardName)) throw new ArgumentException("Ward name is required.");

                if (ward.WardId == 0)
                {
                    string sql = "INSERT INTO Wards (WardName, Floor, Wing, IsActive) VALUES (@WardName, @Floor, @Wing, @IsActive)";
                    _db.ExecuteNonQuery(sql, new[] {
                        new SqlParameter("@WardName", ward.WardName),
                        new SqlParameter("@Floor", (object?)ward.Floor ?? DBNull.Value),
                        new SqlParameter("@Wing", (object?)ward.Wing ?? DBNull.Value),
                        new SqlParameter("@IsActive", ward.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE Wards SET WardName=@WardName, Floor=@Floor, Wing=@Wing, IsActive=@IsActive WHERE WardId=@WardId";
                    _db.ExecuteNonQuery(sql, new[] {
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

        // --- Room Types ---
        public List<RoomType> GetRoomTypes()
        {
            try
            {
                return _db.ExecuteQuery("SELECT TOP 100 * FROM RoomTypes ORDER BY TypeName", r => new RoomType
                {
                    RoomTypeId = (int)r["RoomTypeId"],
                    TypeName = r["TypeName"].ToString()!,
                    DailyRate = (decimal)r["DailyRate"],
                    Description = r["Description"]?.ToString() ?? ""
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving room types: {ex.Message}", ex);
            }
        }

        public List<RoomType> GetRoomTypesPaged(int skip, int take, string orderBy)
        {
            try
            {
                string orderClause = string.IsNullOrEmpty(orderBy) ? "TypeName" : orderBy;
                string sql = $@"SELECT * FROM RoomTypes 
                                ORDER BY {orderClause} 
                                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
                return _db.ExecuteQuery(sql, r => new RoomType
                {
                    RoomTypeId = (int)r["RoomTypeId"],
                    TypeName = r["TypeName"].ToString()!,
                    DailyRate = (decimal)r["DailyRate"],
                    Description = r["Description"]?.ToString() ?? ""
                }, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
            }
            catch { return new List<RoomType>(); }
        }

        public int GetRoomTypesCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM RoomTypes") ?? 0);

        public void SaveRoomType(RoomType type)
        {
            try
            {
                if (type == null || string.IsNullOrEmpty(type.TypeName)) throw new ArgumentException("Room type name is required.");

                if (type.RoomTypeId == 0)
                {
                    string sql = "INSERT INTO RoomTypes (TypeName, DailyRate, Description) VALUES (@TypeName, @DailyRate, @Description)";
                    _db.ExecuteNonQuery(sql, new[] {
                        new SqlParameter("@TypeName", type.TypeName),
                        new SqlParameter("@DailyRate", type.DailyRate),
                        new SqlParameter("@Description", (object?)type.Description ?? DBNull.Value)
                    });
                }
                else
                {
                    string sql = "UPDATE RoomTypes SET TypeName=@TypeName, DailyRate=@DailyRate, Description=@Description WHERE RoomTypeId=@RoomTypeId";
                    _db.ExecuteNonQuery(sql, new[] {
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

        // --- Rooms ---
        public List<Room> GetRooms()
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

                return _db.ExecuteQuery(sql, r => new Room
                {
                    RoomId = (int)r["RoomId"],
                    WardId = r["WardId"] != DBNull.Value ? (int)r["WardId"] : 0,
                    RoomNumber = r["RoomNumber"].ToString()!,
                    RoomTypeId = r["RoomTypeId"] != DBNull.Value ? (int)r["RoomTypeId"] : 0,
                    IsActive = (bool)r["IsActive"],
                    WardName = r["WardName"]?.ToString() ?? "Unknown",
                    RoomTypeName = r["RoomTypeName"]?.ToString() ?? "Standard"
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving rooms: {ex.Message}", ex);
            }
        }

        public List<Room> GetRoomsPaged(int skip, int take, string orderBy)
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

                return _db.ExecuteQuery(sql, r => new Room
                {
                    RoomId = (int)r["RoomId"],
                    WardId = r["WardId"] != DBNull.Value ? (int)r["WardId"] : 0,
                    RoomNumber = r["RoomNumber"].ToString()!,
                    RoomTypeId = r["RoomTypeId"] != DBNull.Value ? (int)r["RoomTypeId"] : 0,
                    IsActive = (bool)r["IsActive"],
                    WardName = r["WardName"]?.ToString() ?? "Unknown",
                    RoomTypeName = r["RoomTypeName"]?.ToString() ?? "Standard"
                }, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
            }
            catch { return new List<Room>(); }
        }

        public int GetRoomsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Rooms WHERE IsActive = 1") ?? 0);

        public void SaveRoom(Room room)
        {
            try
            {
                if (room == null || string.IsNullOrEmpty(room.RoomNumber)) throw new ArgumentException("Room number is required.");

                if (room.RoomId == 0)
                {
                    string sql = "INSERT INTO Rooms (WardId, RoomNumber, RoomTypeId, IsActive) VALUES (@WardId, @RoomNumber, @RoomTypeId, @IsActive)";
                    _db.ExecuteNonQuery(sql, new[] {
                        new SqlParameter("@WardId", room.WardId),
                        new SqlParameter("@RoomNumber", room.RoomNumber),
                        new SqlParameter("@RoomTypeId", room.RoomTypeId),
                        new SqlParameter("@IsActive", room.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE Rooms SET WardId=@WardId, RoomNumber=@RoomNumber, RoomTypeId=@RoomTypeId, IsActive=@IsActive WHERE RoomId=@RoomId";
                    _db.ExecuteNonQuery(sql, new[] {
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

        // --- Beds ---
        public List<Bed> GetBeds()
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

                return _db.ExecuteQuery(sql, r => new Bed
                {
                    BedId = (int)r["BedId"],
                    RoomId = r["RoomId"] != DBNull.Value ? (int)r["RoomId"] : 0,
                    BedNumber = r["BedNumber"].ToString()!,
                    Status = r["Status"].ToString()!,
                    IsActive = (bool)r["IsActive"],
                    RoomNumber = r["RoomNumber"]?.ToString() ?? "N/A",
                    WardName = r["WardName"]?.ToString() ?? "N/A"
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving beds: {ex.Message}", ex);
            }
        }

        public List<Bed> GetBedsPaged(int skip, int take, string orderBy)
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

                return _db.ExecuteQuery(sql, r => new Bed
                {
                    BedId = (int)r["BedId"],
                    RoomId = r["RoomId"] != DBNull.Value ? (int)r["RoomId"] : 0,
                    BedNumber = r["BedNumber"].ToString()!,
                    Status = r["Status"].ToString()!,
                    IsActive = (bool)r["IsActive"],
                    RoomNumber = r["RoomNumber"]?.ToString() ?? "N/A",
                    WardName = r["WardName"]?.ToString() ?? "N/A"
                }, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
            }
            catch { return new List<Bed>(); }
        }

        public int GetBedsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Beds WHERE IsActive = 1") ?? 0);

        public void SaveBed(Bed bed)
        {
            try
            {
                if (bed == null || string.IsNullOrEmpty(bed.BedNumber)) throw new ArgumentException("Bed number is required.");

                if (bed.BedId == 0)
                {
                    string sql = "INSERT INTO Beds (RoomId, BedNumber, Status, IsActive) VALUES (@RoomId, @BedNumber, @Status, @IsActive)";
                    _db.ExecuteNonQuery(sql, new[] {
                        new SqlParameter("@RoomId", bed.RoomId),
                        new SqlParameter("@BedNumber", bed.BedNumber),
                        new SqlParameter("@Status", bed.Status ?? "Available"),
                        new SqlParameter("@IsActive", bed.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE Beds SET RoomId=@RoomId, BedNumber=@BedNumber, Status=@Status, IsActive=@IsActive WHERE BedId=@BedId";
                    _db.ExecuteNonQuery(sql, new[] {
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

        // PERFORMANCE OPTIMIZATION: Load rooms only for specific ward
        public List<Room> GetRoomsByWard(int wardId)
        {
            try
            {
                string sql = @"
                    SELECT r.*, w.WardName, rt.TypeName as RoomTypeName 
                    FROM Rooms r
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    LEFT JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
                    WHERE r.IsActive = 1 AND r.WardId = @WardId";

                return _db.ExecuteQuery(sql, r => new Room
                {
                    RoomId = (int)r["RoomId"],
                    WardId = r["WardId"] != DBNull.Value ? (int)r["WardId"] : 0,
                    RoomNumber = r["RoomNumber"].ToString()!,
                    RoomTypeId = r["RoomTypeId"] != DBNull.Value ? (int)r["RoomTypeId"] : 0,
                    IsActive = (bool)r["IsActive"],
                    WardName = r["WardName"]?.ToString() ?? "Unknown",
                    RoomTypeName = r["RoomTypeName"]?.ToString() ?? "Standard"
                }, new[] { new SqlParameter("@WardId", wardId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving rooms for ward {wardId}: {ex.Message}", ex);
            }
        }

        // PERFORMANCE OPTIMIZATION: Load only available beds for specific room
        public List<Bed> GetAvailableBedsByRoom(int roomId)
        {
            try
            {
                string sql = @"
                    SELECT b.*, r.RoomNumber, w.WardName 
                    FROM Beds b
                    LEFT JOIN Rooms r ON b.RoomId = r.RoomId
                    LEFT JOIN Wards w ON r.WardId = w.WardId
                    WHERE b.IsActive = 1 AND b.RoomId = @RoomId AND b.Status = 'Available'";

                return _db.ExecuteQuery(sql, r => new Bed
                {
                    BedId = (int)r["BedId"],
                    RoomId = r["RoomId"] != DBNull.Value ? (int)r["RoomId"] : 0,
                    BedNumber = r["BedNumber"].ToString()!,
                    Status = r["Status"].ToString()!,
                    IsActive = (bool)r["IsActive"],
                    RoomNumber = r["RoomNumber"]?.ToString() ?? "N/A",
                    WardName = r["WardName"]?.ToString() ?? "N/A"
                }, new[] { new SqlParameter("@RoomId", roomId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving available beds for room {roomId}: {ex.Message}", ex);
            }
        }

        // --- Admissions ---
        public void AdmitPatient(Admission admission)
        {
            try
            {
                if (admission == null || admission.PatientId <= 0 || admission.BedId <= 0)
                    throw new ArgumentException("Invalid admission data. Patient and Bed are required.");

                // Update Bed Status first
                string updateBedSql = "UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId";
                _db.ExecuteNonQuery(updateBedSql, new[] { new SqlParameter("@BedId", admission.BedId) });

                // Create Admission
                string sql = @"
                    INSERT INTO Admissions (PatientId, BedId, AdmissionDate, Status, Notes) 
                    VALUES (@PatientId, @BedId, @AdmissionDate, 'Admitted', @Notes)";

                _db.ExecuteNonQuery(sql, new[] {
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

        public void DischargePatient(int admissionId)
        {
            try
            {
                if (admissionId <= 0) return;

                // Get BedId first
                var bedIdObj = _db.ExecuteScalar("SELECT BedId FROM Admissions WHERE AdmissionId = @Id", new[] { new SqlParameter("@Id", admissionId) });
                if (bedIdObj != null && int.TryParse(bedIdObj.ToString(), out int bedId))
                {
                    // Update Bed Status to Cleaning
                    _db.ExecuteNonQuery("UPDATE Beds SET Status = 'Cleaning' WHERE BedId = @BedId", new[] { new SqlParameter("@BedId", bedId) });
                }

                // Update Admission
                string sql = "UPDATE Admissions SET DischargeDate = GETDATE(), Status = 'Discharged' WHERE AdmissionId = @Id";
                _db.ExecuteNonQuery(sql, new[] { new SqlParameter("@Id", admissionId) });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to discharge admission {admissionId}: {ex.Message}", ex);
            }
        }

        public List<Admission> GetActiveAdmissions()
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

                return _db.ExecuteQuery(sql, MapAdmission);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving active admissions: {ex.Message}", ex);
            }
        }

        public List<Admission> GetActiveAdmissionsPaged(int skip, int take, string orderBy)
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

                return _db.ExecuteQuery(sql, MapAdmission, new[] { new SqlParameter("@Skip", skip), new SqlParameter("@Take", take) });
            }
            catch { return new List<Admission>(); }
        }

        public int GetActiveAdmissionsCount() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Admissions WHERE Status = 'Admitted'") ?? 0);

        private Admission MapAdmission(DataRow r)
        {
            return new Admission
            {
                AdmissionId = (int)r["AdmissionId"],
                PatientId = (int)r["PatientId"],
                BedId = (int)r["BedId"],
                AdmissionDate = (DateTime)r["AdmissionDate"],
                Status = r["Status"]?.ToString() ?? "Admitted",
                Notes = r["Notes"]?.ToString() ?? "",
                BedNumber = r["BedNumber"]?.ToString() ?? "N/A",
                RoomNumber = r["RoomNumber"]?.ToString() ?? "N/A",
                WardName = r["WardName"]?.ToString() ?? "N/A",
                RoomTypeName = r["RoomTypeName"]?.ToString() ?? "N/A",
                DailyRate = r["DailyRate"] != DBNull.Value ? (decimal)r["DailyRate"] : 0,
                PatientName = r.Table.Columns.Contains("PatientName") ? r["PatientName"]?.ToString() : "Unknown",
                HasPendingBill = Convert.ToInt32(r["PendingBillCount"]) > 0
            };
        }

        public List<OperationTheater> GetTheaters()
        {
            try
            {
                return _db.ExecuteQuery("SELECT * FROM OperationTheaters WHERE IsActive = 1", r => new OperationTheater
                {
                    TheaterId = (int)r["TheaterId"],
                    TheaterName = r["TheaterName"]?.ToString() ?? "Unknown OT",
                    Status = r["Status"]?.ToString() ?? "Available",
                    IsActive = (bool)r["IsActive"]
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving operation theaters: {ex.Message}", ex);
            }
        }

        public void SaveTheater(OperationTheater theater)
        {
            try
            {
                if (theater == null || string.IsNullOrEmpty(theater.TheaterName)) throw new ArgumentException("Theater name is required.");

                if (theater.TheaterId == 0)
                {
                    string sql = "INSERT INTO OperationTheaters (TheaterName, Status, IsActive) VALUES (@TheaterName, @Status, @IsActive)";
                    _db.ExecuteNonQuery(sql, new[] {
                        new SqlParameter("@TheaterName", theater.TheaterName),
                        new SqlParameter("@Status", theater.Status ?? "Available"),
                        new SqlParameter("@IsActive", theater.IsActive)
                    });
                }
                else
                {
                    string sql = "UPDATE OperationTheaters SET TheaterName=@TheaterName, Status=@Status, IsActive=@IsActive WHERE TheaterId=@TheaterId";
                    _db.ExecuteNonQuery(sql, new[] {
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

        public void UpdateTheaterStatus(int theaterId, string status)
        {
            try
            {
                if (theaterId <= 0) return;
                string sql = "UPDATE OperationTheaters SET Status = @Status WHERE TheaterId = @TheaterId";
                _db.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@TheaterId", theaterId),
                    new SqlParameter("@Status", status)
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update theater {theaterId} status: {ex.Message}", ex);
            }
        }
    }
}
