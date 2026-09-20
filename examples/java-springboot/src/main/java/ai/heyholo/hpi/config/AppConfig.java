package ai.heyholo.hpi.config;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class AppConfig {

    private static final Logger logger = LoggerFactory.getLogger(AppConfig.class);

    @Bean
    public CommandLineRunner startupMessage() {
        return args -> {
            logger.info("🚀 HeyHolo POS Interface - Java Spring Boot Example");
            logger.info("📡 Server starting on http://localhost:5000");
            logger.info("📝 Endpoints available:");
            logger.info("   GET  /products");
            logger.info("   POST /orders");
            logger.info("   GET  /orders/{id}");
            logger.info("   POST /orders/{id}/cancel");
            logger.info("   GET  /locations");
        };
    }
}
