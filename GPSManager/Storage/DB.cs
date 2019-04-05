using GPSManager.Polygons;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GPSManager.Storage
{
    static class DB
    {
        private const string DataSource = "192.168.55.125";
        private const string DatabaseName = "RIT_MAP";
        private const string Username = "sa";
        private const string Password = "1234";
        private const string PolygonsTable = "POLYGONS";

        private static List<Polygon> polygons;

        public static IReadOnlyList<Polygon> Polygons => polygons;

        public static void Load()
        {
            using (var connection = OpenConnection())
            {
                polygons = LoadPolygons(connection).ToList();
            }
        }

        /// <summary>
        /// Inserts the polygon into the database. Returns the ID the database assigned to the new record
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static int InsertPolygonAndAssingID(Polygon polygon)
        {
            if(polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon));
            }
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO POLYGONS (NAME, GEOMETRY) VALUES (@name, @geometry) " +
                                      "SELECT SCOPE_IDENTITY()";
                command.Parameters.AddWithValue("@name", polygon.Name ?? "");
                command.Parameters.AddWithValue("@geometry", polygon.GeometryText);
                var id = (int)(decimal)command.ExecuteScalar();
                polygon.ID = id;
                polygons.Add(polygon);
                return id;
            }
        }

        public static bool RemovePolygon(Polygon polygon)
        {
            if(polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon));
            }
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                int id = polygon.ID;
                command.CommandText = "DELETE FROM POLYGONS WHERE ID = @id";
                command.Parameters.AddWithValue("@id", id);
                bool removed = 1 == command.ExecuteNonQuery();
                if(removed)
                {
                    polygons.Remove(polygon);
                }
                return removed;
            }
        }

        private static SqlConnection OpenConnection()
        {
            var connectionString = $"Server={DataSource};Initial Catalog={DatabaseName};" +
                $"Persist Security Info=True;User ID={Username};Password={Password}";

            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static IEnumerable<Polygon> LoadPolygons(SqlConnection connection)
        {
            using (var adapter = new SqlDataAdapter())
            {
                string sql = $"SELECT ID, NAME, GEOMETRY FROM {PolygonsTable}";
                adapter.SelectCommand = new SqlCommand(sql, connection);
                using (var table = new DataTable())
                {
                    adapter.Fill(table);
                    var argException = new List<ArgumentException>();
                    foreach (DataRow row in table.Rows)
                    {
                        Polygon polygon = null;
                        try
                        {
                            polygon = GetPolygonFromRow(row);
                        }
                        catch (ArgumentException ex)
                        {
                            argException.Add(ex);
                        }
                        if(polygon != null)
                        {
                            yield return polygon;
                        }
                    }
                    if(argException.Any())
                    {
                        var msg = string.Join(";\n", argException.Select(ex => ex.Message));
                        MessageBox.Show("При загрузке полигонов произошли ошибки:\n" + msg, 
                                        "Ошибка загрузки полигонов", 
                                        MessageBoxButton.OK, 
                                        MessageBoxImage.Error);
                    }
                }
            }
        }

        private static Polygon GetPolygonFromRow(DataRow row)
        {
            return Polygon.FromGeomText(row.Field<string>("GEOMETRY"),
                                        row.Field<int>("ID"),
                                        row.Field<string>("NAME"));
        }

        public static bool UpdatePolygon(Polygon polygon)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon));
            }
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE {PolygonsTable} SET " +
                    $"NAME = @name, GEOMETRY = @geometry WHERE ID = @id";
                command.Parameters.AddWithValue("@name", polygon.Name ?? "");
                command.Parameters.AddWithValue("@geometry", polygon.GeometryText);
                command.Parameters.AddWithValue("@id", polygon.ID);
                bool updated = 1 == command.ExecuteNonQuery();
                if (updated)
                {
                    var oldPolygonWithSameID = polygons.Find(p => p.ID == polygon.ID);
                    if(oldPolygonWithSameID != polygon)
                    {
                        polygons.Remove(oldPolygonWithSameID);
                        polygons.Add(polygon);
                    }
                }
                return updated;
            }
        }
    }
}
