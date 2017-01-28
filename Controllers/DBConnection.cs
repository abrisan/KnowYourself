using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using QC = System.Data.SqlClient;
using CG = System.Collections.Generic;
using TD = System.Threading;

namespace KnowYourself.Controllers
{
    public class DBConnection {

        private QC.SqlConnection conn;

        public DBConnection()
        {
            conn = getConnection();
        }

        static QC.SqlConnection getConnection()
        {
            return new QC.SqlConnection("Server = tcp:knowyourself.database.windows.net, 1433; Initial Catalog = KnowYourself; Persist Security Info = False; User ID = su ; Password = !alexandrU97 ; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30");
            
        }

        public Dictionary<String, List<String>> query_table_for_id(String table_name, String id)
        {
            var command = new QC.SqlCommand();
            command.Connection = this.conn;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = @"
                SELECT * FROM @Name WHERE id = @Id
            ";
            var param1 = new QC.SqlParameter("Name", table_name);
            var param2 = new QC.SqlParameter("Id", id);

            command.Parameters.Add(param1);
            command.Parameters.Add(param2);

            QC.SqlDataReader reader = command.ExecuteReader();

            Dictionary<String, List<String>> ret = new Dictionary<string, List<string>>();

            while (reader.Read())
            {
                List<String> adder = new List<string>();
                for(int i = 1; i < reader.FieldCount; ++i)
                {
                    adder.Add(reader.GetString(i));
                }
                ret.Add(reader.GetString(0), adder);
            }

            return ret;
        }

        public int InsertUser(string slack_id)
        {
            var command = new QC.SqlCommand();
            command.Connection = this.conn;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = @"
                SELECT TOP(1) id
                FROM users
                ORDER BY id
            ";
            int n;
            try
            {
                n = command.ExecuteReader().GetInt32(0) + 1;
            }
            catch(Exception e)
            {
                n = 0;
            }
            try
            {
                var command2 = new QC.SqlCommand();
                command2.Connection = this.conn;
                command2.CommandType = System.Data.CommandType.Text;
                command2.CommandText = @"
                    INSERT INTO users (id, slack_id)
                    VALUES (@id, @slack_id)
                    ";
                var param1 = new QC.SqlParameter("id", n);
                var param2 = new QC.SqlParameter("slack_id", slack_id);
                command2.Parameters.Add(param1);
                command2.Parameters.Add(param2);
                int succ = command2.ExecuteNonQuery();
                return succ;
            }
            catch(Exception e)
            {
                return -1;
            }
        }

    }


}


