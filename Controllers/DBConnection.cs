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
            conn.Open();

        }

        static QC.SqlConnection getConnection()
        {
            return new QC.SqlConnection(@"Server = tcp:knowyourself.database.windows.net, 1433; 
                    Initial Catalog = KnowYourself; Persist Security Info = False; 
                    User ID = su ; Password = !alexandrU97 ; 
                    MultipleActiveResultSets = True; Encrypt = True; 
                    TrustServerCertificate = False; 
                    Connection Timeout = 30");
            
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

        public List<String> InsertUser(string slack_id)
        {
            
            List<String> messages = new List<String>();
            try
            {
                var command2 = new QC.SqlCommand();
                command2.Connection = this.conn;
                command2.CommandType = System.Data.CommandType.Text;
                command2.CommandText = @"
                    INSERT INTO users (slack_id)
                    VALUES (@slack_id)
                    ";
                var param2 = new QC.SqlParameter("slack_id", slack_id);
                command2.Parameters.Add(param2);
                int succ = command2.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                messages.Add(e.Message);
            }
            return messages;
        }

        public List<String> GetDiseases()
        {
            List<String> ret = new List<String>();

            try
            {
                QC.SqlCommand com = new QC.SqlCommand();
                com.Connection = this.conn;
                com.CommandType = System.Data.CommandType.Text;
                com.CommandText = @"SELECT disease_name FROM diseases";
                QC.SqlDataReader result = com.ExecuteReader();
                while (result.Read())
                {
                    ret.Add(result.GetString(0));
                }
            }catch(Exception e)
            {
                ret.Add(e.Message);
            }


            return ret;
        }

        public bool UserExists(string slack_id)
        {
            QC.SqlCommand exists = new QC.SqlCommand();
            exists.CommandType = System.Data.CommandType.Text;
            exists.Connection = this.conn;
            exists.CommandText = @"SELECT * FROM users WHERE slack_id = @User";

            var param1 = new QC.SqlParameter("User", slack_id);
            exists.Parameters.Add(param1);

            try
            {
                int id = (int)exists.ExecuteScalar();
                if (id > 0) return true;
               
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }


    }


}


